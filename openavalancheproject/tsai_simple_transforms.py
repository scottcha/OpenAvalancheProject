__all__ = ['TSFilter', 'TSSimpleStandardize', 'TSSimpleNormalize']

from tsai.all import Transform, TSTensor, torch
import numpy as np
class TSFilter(Transform):
    def __init__(self, indexes_to_include=None, days=28):
        #assert indexes_to_include is not None
        
        self.indexes_to_include = indexes_to_include
        self.days = days
    
    def encodes(self, o:TSTensor):
        #start_index = 1441 - (self.days * 8)
        if self.indexes_to_include is None:            
            #return o[:, :, start_index:]        
            return o[:, :, :]        
        else:
            #return o[:, self.indexes_to_include, start_index:]     
            return o[:, self.indexes_to_include, :]   

class TSSimpleStandardize(Transform):
    def __init__(self, mean=None, std=None):
        assert mean is not None
        assert std is not None
        
        self.mean = mean
        self.std = std
    
    def encodes(self, o:TSTensor):        
        o = o - torch.tensor(self.mean[np.newaxis, :, np.newaxis], device=o.device)
        o = o / torch.tensor(self.std[np.newaxis, :, np.newaxis], device=o.device)
        return o 

class TSSimpleNormalize(Transform):
    def __init__(self, mins=None, maxs=None):
        assert mins is not None
        assert maxs is not None
        
        self.mins = mins
        self.maxs = maxs
    
    def encodes(self, o:TSTensor):        
        o_min = torch.tensor(self.mins[np.newaxis, :, np.newaxis], device=o.device)
        o = o - o_min
        o = o / (torch.tensor(self.maxs[np.newaxis, :, np.newaxis], device=o.device) - o_min)
        return o