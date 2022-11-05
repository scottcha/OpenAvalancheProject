import numpy as np
import scipy
from openavalancheproject.tsai_utilities import *
from tsai.all import * 
class OAPStats():
    def check_distributions(self, X, y_df, filtered_feature_list):
        '''
        Check which seasons are most representative of the larger dataset
        Checks wilcoxon stat as well as mean and media differences
        If null hypothesis is rejected on wilcoxon stat it implies the distributions are likely different
        '''
        for holdout in y_df['season'].unique():
            sample = y_df[y_df['season'] != holdout].sample(10000).index
            sample_holdout = y_df[y_df['season'] == holdout].sample(10000).index
            print(holdout)
            results = []
            for i in range(X.shape[1]):
                try:
                    s1 = np.nan_to_num(np.matrix.flatten(X[sample, i, :]))
                    sh = np.nan_to_num(np.matrix.flatten(X[sample_holdout, i, :]))
                    result = scipy.stats.wilcoxon(s1, sh)
                    result_median = np.median(s1)
                    holdout_median = np.median(sh)
                    result_mean = np.mean(s1)
                    holdout_mean = np.mean(sh)
                    results.append((filtered_feature_list[i], result[0], result[1], abs(result_median-holdout_median), abs(result_mean - holdout_mean)))
                except:
                    #catch the error where the sample values are the same
                    continue
            stats_df = pd.DataFrame().from_records(results, columns=['feature', 'stat', 'p-value', 'median_diff', 'mean_diff'])
            stats_df['RejectNull'] = stats_df.apply(lambda x: x['p-value'] < .05, axis=1)
            print(stats_df.groupby('RejectNull').size())
            print('Median_diff ' + str(stats_df['median_diff'].sum()))
            print('Mean_diff ' + str(stats_df['mean_diff'].sum()))

    def generate_learning_curve(self, utils:TSAIUtilities, splits3 = None):
        '''
        Steps through 4 subsets of splits3 in increasing increments to show the learning curve 
        '''
        assert splits3 != None
        results = []
        for i in range(0,100,25):
            sample_frac = .001
            if i != 0:
                sample_frac = i / 1000.0
            splits, dls = utils.create_dls(splits=splits3, sample_frac=sample_frac)
            model = InceptionTimePlus(dls.vars, dls.c)
            learn = Learner(dls, model, metrics=[accuracy, BalancedAccuracy()], loss_func=None, cbs=[ShowGraphCallback2(), PredictionDynamics()]) 
            learn.fit(25, lr=1e-3)
            splits, dls = utils.create_dls(splits=splits)
            probas, target, preds = learn.get_preds(dl=dls.valid, with_input=False, with_loss=False, with_decoded=True, act=None, reorder=False)
            valid_score = skm.accuracy_score(target, preds)
            splits, dls = utils.create_dls(splits=splits)
            probas, target, preds = learn.get_preds(dl=dls.train, with_input=False, with_loss=False, with_decoded=True, act=None, reorder=False)
            train_score = skm.accuracy_score(target, preds)
            t = (i, 1-valid_score, 1-train_score)
            results.append(t)
            print(t)
            df = pd.DataFrame.from_records(results, columns=['index', 'valid', 'train'])
            df.set_index('index', inplace=True)
            df.plot()
            plt.show()
    
    def show_feature_distributions(self, X):
        for i in range(0, X.shape[1]):
            t =  X[:, i, :]
            t2 = np.reshape(t, t.shape[0] * t.shape[1])
            pd.Series(t2).hist(bins=10)
            plt.show()
