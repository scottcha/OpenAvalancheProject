from tsai.all import *
from openavalancheproject.tsai_utilities import *
import argparse
from azureml.core import Run

data_root = '/media/scottcha/E1/Data/OAPMLData/'

ml_path = data_root + '/5.MLData/'
num_features = 978
interpolation = 1

parser = argparse.ArgumentParser(description='LSTM Train')
parser.add_argument('--epochs', type=int, default=5)
parser.add_argument('--label', type=str, default='Day1DangerAboveTreeline')
parser.add_argument('--batch_size', type=int, default=128)
parser.add_argument('--hidden', type=int, default=100)
parser.add_argument('--dropout', type=float, default=0)
parser.add_argument('--fc_dropout', type=float, default=0)
parser.add_argument('--layers', type=float, default=3)
parser.add_argument('--bidirectional', type=int, default=False)
parser.add_argument('--model_index', type=int, default=0)

args = parser.parse_args()

class TSStandardizeNanMeanReplaceNan(Transform):
    #method to standardize each batch while also replacing any nans with the mean value before standarization
    "Standardize/destd batch of `NumpyTensor` or `TSTensor`"
    parameters, order = L('mean', 'std'), 99
    def __init__(self, mean=None, feature_means=None, std=None, feature_std=None, by_sample=False, by_var=False, verbose=False):
        self.mean = tensor(mean) if mean is not None else None
        self.std = tensor(std) if std is not None else None
        self.feature_means = feature_means
        self.feature_std = feature_std
        self.by_sample, self.by_var = by_sample, by_var
        if by_sample and by_var: self.axes = (2)
        elif by_sample: self.axes = (1, 2)
        elif by_var: self.axes = (0, 2)
        else: self.axes = ()
        self.verbose = verbose

    @classmethod
    def from_stats(cls, mean, std): return cls(mean, std)

    def setups(self, dl: DataLoader):
        if self.mean is None or self.std is None:
            pv(f'{self.__class__.__name__} setup mean={self.mean}, std={self.std}, by_sample={self.by_sample}, by_var={self.by_var}', self.verbose)
            x, *_ = dl.one_batch()
            x = torch.where(torch.isnan(x), torch.zeros_like(x), x)
            self.mean, self.std = x.mean(self.axes, keepdim=self.axes!=()), x.std(self.axes, keepdim=self.axes!=()) + 1e-7
            pv(f'mean: {self.mean}  std: {self.std}\n', self.verbose)

    def encodes(self, x:(NumpyTensor, TSTensor)):
        fill_values = torch.zeros_like(x)
        std_values = torch.zeros_like(x)       
        for i in range(0,x.shape[1]):
            fill_values[:,i,:] = torch.full_like(x[:,i,:], self.feature_means[i])
            std_values[:,i,:] = torch.full_like(x[:,i,:], self.feature_std[i])
        
        x = torch.where(torch.isnan(x), fill_values, x)
        
        if self.by_sample:        
            self.mean, self.std = x.mean(self.axes, keepdim=self.axes!=()), x.std(self.axes, keepdim=self.axes!=()) + 1e-7
            
        t = (x - fill_values) / std_values
        del fill_values, std_values
        return torch.where(torch.isnan(t), torch.zeros_like(t), t)

l = args.label 
file_label = 'co_' + l + '_small'
fname = ml_path + '/X_all_' + file_label + '.npy'
        
X = np.load(fname, mmap_mode='r')
X = X[:,:,-30:]
utils = TSAIUtilities(X, l)

means_fn = ml_path + '/feature_means_interpolation' + str(interpolation) + '_' + file_label + 'x.npy'
std_fn = ml_path + '/feature_std_interpolation' + str(interpolation)  + '_' + file_label +   'x.npy'

feature_means = utils.get_feature_means(from_cache=means_fn)
feature_std = utils.get_feature_std(from_cache=std_fn)

i=0
y_train_df = pd.read_parquet(ml_path + '/y_train_batch_' + str(i) + '_' + file_label + '.parquet')  
y_test_df = pd.read_parquet(ml_path + '/y_test_batch_' + str(i) + '_' + file_label + '.parquet')  
y_df = pd.concat([y_train_df, y_test_df]).reset_index(drop=True)
y, cat_dict = utils.get_y_as_cat(y_df)

train_test_split = 5000
num_y = 1000

splits_2 = (L([i for i in range(0,train_test_split)]).shuffle(), L([i for i in range(train_test_split,train_test_split+num_y)]).shuffle())
    
tfms = [None, [Categorize()]]
dsets = TSDatasets(X, y, tfms=tfms, splits=splits_2, inplace=True)
#create the dataloader
dls = TSDataLoaders.from_dsets(dsets.train, dsets.valid, bs=[64], batch_tfms=[TSStandardizeNanMeanReplaceNan(feature_means=feature_means, feature_std=feature_std)], num_workers=0)

h = args.hidden 
l = args.layers 
d = args.dropout 
b = False
if args.bidirectional == 1:
    b = True
f = args.fc_dropout 
models =  [LSTM(dls.vars, dls.c, hidden_size=h, n_layers=l, rnn_dropout=d, bidirectional=b, fc_dropout=f),
           LSTMPlus(dls.vars, dls.c, hidden_size=h, n_layers=l, rnn_dropout=d, bidirectional=b,fc_dropout=f)]

m = models[args.model_index]
learn = Learner(dls, m, metrics=accuracy)
learn.fit_one_cycle(5, lr_max=1e-2, cbs=EarlyStoppingCallback(monitor='valid_loss', min_delta=0.05, patience=2))
run = Run.get_context()
run.log('Model', str(type(m)))
run.log('Num Hidden ', h)
run.log('Num Layers ', l)
run.log('RNN Dropout ',d)
run.log('FC Dropout ', f)
run.log('Bidirectional ',b)
interp = ClassificationInterpretation.from_learner(learn)
cm = interp.confusion_matrix()
cm = cm.astype('float') / cm.sum(axis=1)[:, np.newaxis]
for r in range(len(cm)):
    for c in range(len(cm[r])):
        run.log('Actual ' + str(r) + ' Predicted ' + str(c), cm[r,c])

d1,t1 = flatten_check(interp.decoded, interp.targs)
run.log('Test Accuracy', skm.accuracy_score(t1, d1))
