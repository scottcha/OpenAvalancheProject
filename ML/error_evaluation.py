import itertools
import numpy as np
import pandas as pd
from sklearn.metrics import confusion_matrix, accuracy_score, classification_report 
#import matplotlib
import matplotlib.pyplot as plt
from sklearn.preprocessing import Normalizer

def evaluateSingleClass(y_test, result):
    cm = confusion_matrix(y_test, result)
    print(cm)
    accuracy = accuracy_score(y_test.values, result)
    print("Accuracy: " + str(accuracy))
    print("Classification Report")
    print(classification_report(y_test.values, result))
    return classification_report(y_test.values, result, output_dict=True)

def evaluateSingleClassShort(y_test, result):
    accuracy = accuracy_score(y_test.values, result)
    print("Accuracy: " + str(accuracy))
    print("Classification Report")
    print(classification_report(y_test.values, result))
    return classification_report(y_test.values, result, output_dict=True)

#nice confustion matrix plot taken from scikit-learn docs
def plot_confusion_matrix(cm, classes,
                          normalize=False,
                          title='Confusion matrix',
                          cmap=plt.cm.Blues):
    """
    This function prints and plots the confusion matrix.
    Normalization can be applied by setting `normalize=True`.
    """

    fig = plt.figure()
    if normalize:
        cm = cm.astype('float') / cm.sum(axis=1)[:, np.newaxis]
        print("Normalized confusion matrix")
    else:
        print('Confusion matrix, without normalization')

    plt.imshow(cm, interpolation='nearest', cmap=cmap)
    plt.title(title)
    plt.colorbar()
    tick_marks = np.arange(len(classes))
    plt.xticks(tick_marks, classes, rotation=45)
    plt.yticks(tick_marks, classes)

    fmt = '.2f' if normalize else 'd'
    thresh = cm.max() / 2.
    for i, j in itertools.product(range(cm.shape[0]), range(cm.shape[1])):
        plt.text(j, i, format(cm[i, j], fmt),
                 horizontalalignment="center",
                 color="white" if cm[i, j] > thresh else "black")

    plt.tight_layout()
    plt.ylabel('True label')
    plt.xlabel('Predicted label')
    return fig

def add_location_to_prediciton(row, region_df):
    regions = region_df['Region'].values
    for x in regions:
        min_maxes = region_df[region_df['Region'] == x]
        if(row['Lat'] > min_maxes['LatMin'] and row['Lat'] < min_maxes['LatMax'] and row['Lon'] > min_maxes['LonMax'] and row['Lon'] < min_maxes['LonMax']):
            row['Region'] = x  

#TODO: refactor to read this from a file and add other avy centers
def set_critical_points(row):
    #Olympics: Hurricane Ridge: -123.4724, 47.98101
    #West North: Mt Baker Ski Area: -121.7387, 43.83084
    #West Central: Between Mt Loop and Glacier Peak: -121.2094, 48.08577
    #West South: Paradise: -121.7392, 46.78271
    #Snoqualmie Pass: Alpental: -121.47762, 47.43613
    #Stevens Pass: Stevens Pass North: -121.1233, 47.79041
    #East North: Cutthroat peak/Washington Pass: -120.7334, 48.55459
    #East Central: Enchantments:  -120.8913, 47.51345
    #East South: Mt Adams: -121.5665, 46.18549
    #Mt Hood: Government Camp: -121.7681, 45.32503
    if(row['Lat'] == 47.98101 and row['Lon'] == -123.4724):
        row['CriticalForecastPoint'] = True
    elif(row['Lat'] == 43.83084 and row['Lon'] == -121.7387):
        row['CriticalForecastPoint'] = True
    elif(row['Lat'] == 48.08577 and row['Lon'] == -121.2094):
        row['CriticalForecastPoint'] = True
    elif(row['Lat'] == 46.78271 and row['Lon'] == -121.7392):
        row['CriticalForecastPoint'] = True
    elif(row['Lat'] == 47.43613 and row['Lon'] == -121.47762):
        row['CriticalForecastPoint'] = True
    elif(row['Lat'] == 47.79041 and row['Lon'] == -121.1233):
        row['CriticalForecastPoint'] = True
    elif(row['Lat'] == 48.55459 and row['Lon'] == -120.7334):
        row['CriticalForecastPoint'] = True
    elif(row['Lat'] == 47.51345 and row['Lon'] == -120.8913):
        row['CriticalForecastPoint'] = True
    elif(row['Lat'] == 46.18549 and row['Lon'] == -121.5665):
        row['CriticalForecastPoint'] = True
    elif(row['Lat'] == 45.32503 and row['Lon'] == -121.7681):
        row['CriticalForecastPoint'] = True
    else:
        row['CriticalForecastPoint'] = False
    return row
    