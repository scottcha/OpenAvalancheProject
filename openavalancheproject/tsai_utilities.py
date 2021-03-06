# AUTOGENERATED! DO NOT EDIT! File to edit: DataPipelineNotebooks/4.TSAIUtilities.ipynb (unless otherwise specified).

__all__ = ['TSAIUtilities']

# Cell
from pandas.api.types import CategoricalDtype
#from tsai.all import *
from joblib import Parallel, delayed
import os.path
import numpy as np

# Cell
class TSAIUtilities:
    def __init__(self, X, label):
        self.X = X
        self.num_features = X.shape[1]
        self.label = label

    def _calculate_feature_mean(self, feature_index, num_samples_to_use=5000):
        return np.nanmean(self.X[0:num_samples_to_use,feature_index,:])

    def _calculate_feature_std(self, feature_index, num_samples_to_use=5000):
        return np.nanstd(self.X[0:num_samples_to_use,feature_index,:])

    def get_feature_means(self, from_cache=None):
        if not os.path.isfile(from_cache):
            feature_means = Parallel(n_jobs=4)(map(delayed(self._calculate_feature_mean), range(0,self.num_features)))

            if from_cache is not None:
                np.save(from_cache, np.asarray(feature_means))
        else:
            feature_means = np.load(from_cache)

        return feature_means

    def get_feature_std(self, from_cache=None):
        if not os.path.isfile(from_cache):
            feature_std = Parallel(n_jobs=4)(map(delayed(self._calculate_feature_std), range(0,self.num_features)))

            if from_cache is not None:
                np.save(from_cache, np.asarray(feature_std))

        else:
            feature_std = np.load(from_cache)

        return feature_std

    def get_y_as_cat(self, y_df):
        #convert the labels to encoded values
        labels = y_df[self.label].unique()
        if 'Low' in labels:
            labels = ['Low', 'Moderate', 'Considerable', 'High']
        else:
            labels.sort()
        cat_type = CategoricalDtype(categories=labels, ordered=True)
        y_df[self.label + '_Cat'] = y_df[self.label].astype(cat_type)
        y = y_df[self.label + '_Cat'].cat.codes.values

        cat_dict = dict( enumerate(y_df[self.label + '_Cat'].cat.categories ) )
        return y, cat_dict



# Cell

#class TSStandardizeNanMeanReplaceNan(Transform):
#    #method to standardize each batch while also replacing any nans with the mean value before standarization
#    "Standardize/destd batch of `NumpyTensor` or `TSTensor`"
#    parameters, order = L('mean', 'std'), 99
#    def __init__(self, mean=None, feature_means=None, std=None, feature_std=None, by_sample=False, by_var=False, verbose=False):
#        self.mean = tensor(mean) if mean is not None else None
#        self.std = tensor(std) if std is not None else None
#        self.feature_means = feature_means
#        self.feature_std = feature_std
#        self.by_sample, self.by_var = by_sample, by_var
#        if by_sample and by_var: self.axes = (2)
#        elif by_sample: self.axes = (1, 2)
#        elif by_var: self.axes = (0, 2)
#        else: self.axes = ()
#        self.verbose = verbose

#    @classmethod
#    def from_stats(cls, mean, std): return cls(mean, std)

#    def setups(self, dl: DataLoader):
#        if self.mean is None or self.std is None:
#            pv(f'{self.__class__.__name__} setup mean={self.mean}, std={self.std}, by_sample={self.by_sample}, by_var={self.by_var}', self.verbose)
#            x, *_ = dl.one_batch()
#            x = torch.where(torch.isnan(x), torch.zeros_like(x), x)
#            self.mean, self.std = x.mean(self.axes, keepdim=self.axes!=()), x.std(self.axes, keepdim=self.axes!=()) + 1e-7
#            pv(f'mean: {self.mean}  std: {self.std}\n', self.verbose)

#    def encodes(self, x:(NumpyTensor, TSTensor)):
#        fill_values = torch.zeros_like(x)
#        std_values = torch.zeros_like(x)
#        for i in range(0,x.shape[1]):
#            fill_values[:,i,:] = torch.full_like(x[:,i,:], self.feature_means[i])
#            std_values[:,i,:] = torch.full_like(x[:,i,:], self.feature_std[i])
#
#        x = torch.where(torch.isnan(x), fill_values, x)
#
#        if self.by_sample:
#            self.mean, self.std = x.mean(self.axes, keepdim=self.axes!=()), x.std(self.axes, keepdim=self.axes!=()) + 1e-7
#
#        t = (x - fill_values) / std_values
#        del fill_values, std_values
#        return torch.where(torch.isnan(t), torch.zeros_like(t), t)