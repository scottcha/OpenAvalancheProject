# -*- coding: utf-8 -*-

# Define your item pipelines here
#
# Don't forget to add your pipeline to the ITEM_PIPELINES setting
# See: https://doc.scrapy.org/en/latest/topics/item-pipeline.html


# class AvycenterscrapingPipeline(object):
#     def process_item(self, item, spider):
#         return item
import sys
import scrapy
import PIL
#import settings
from scrapy.pipelines.images import ImagesPipeline
from scrapy.exceptions import DropItem
from PIL import Image
from AvyCenterScraping.items import AvyScrapingItem

class AvycenterscrapingPipeline(ImagesPipeline):

    debug = False 
    #low 76, 185, 66
    #moderate 255, 238, 0
    #considerable 247, 148, 30
    #high 237, 28, 36
    #extreme 0, 0, 0
    def rgb_to_danger(self, r, g, b):
        if(r < 30 and g < 25 and b < 20):
           return "Extreme" + (": " + str(r) + " g:" + str(g) + " b:" + str(b) if self.debug else "")
        elif(r > 200 and g < 50 and b < 60):
            return "High"  + (": " + str(r) + " g:" + str(g) + " b:" + str(b) if self.debug else "") 
        elif(r > 220 and g > 120 and g < 175 and b < 70):
            return "Considerable"  + (": " + str(r) + " g:" + str(g) + " b:" + str(b) if self.debug else "") 
        elif(r > 219 and g > 215 and b < 70):
            return "Moderate"  + (": " + str(r) + " g:" + str(g) + " b:" + str(b) if self.debug else "") 
        elif(r > 45 and r < 110 and g < 220 and b < 110):
            return "Low"  + (": " + str(r) + " g:" + str(g) + " b:" + str(b) if self.debug else "") 
        elif(r > 164 and r < 220 and g > 170 and b > 150):
            return "no-data"  + (": " + str(r) + " g:" + str(g) + " b:" + str(b) if self.debug else "") #sometimes the rose has gray
        else:
            return "Unknown r:" + str(r) + " g:" + str(g) + " b:" + str(b)

    def interpret_rose_graphic(self, rose_file_image, item, problem_type):
        if(problem_type == 'Forecast'): #this type has a legend
            #pixel Coordinates of the image to examine
            northBelowX, northBelowY = 73, 25
            northNearX, northNearY = northBelowX, 37
            northAboveX, northAboveY = northBelowX, 47

            northEastBelowX, northEastBelowY = 104, 36
            northEastNearX, northEastNearY = 91, 44
            northEastAboveX, northEastAboveY = 81, 50

            eastBelowX, eastBelowY = 116, 62
            eastNearX, eastNearY = 99, 58 
            eastAboveX, eastAboveY = 86, 56 

            southEastBelowX, southEastBelowY = 105, 90
            southEastNearX, southEastNearY = 91, 76
            southEastAboveX, southEastAboveY = 81, 64

            southBelowX, southBelowY = 72, 103
            southNearX, southNearY = southBelowX, 84
            southAboveX, southAboveY = southBelowX, 67

            southWestBelowX, southWestBelowY = 38, 89
            southWestNearX, southWestNearY = 52, 75
            southWestAboveX, southWestAboveY = 64, 64

            westBelowX, westBelowY = 28, 58
            westNearX, westNearY = 46, westBelowY
            westAboveX, westAboveY = 58, westBelowY

            northWestBelowX, northWestBelowY = 41, 36
            northWestNearX, northWestNearY = 53, 43
            northWestAboveX, northWestAboveY = 63, 49
        else: #this type has no legend
            #pixel Coordinates of the image to examine
            northBelowX, northBelowY = 92, 32
            northNearX, northNearY = northBelowX, 45 
            northAboveX, northAboveY = northBelowX, 56

            northEastBelowX, northEastBelowY = 129, 43
            northEastNearX, northEastNearY = 113, 51
            northEastAboveX, northEastAboveY = 101, 59

            eastBelowX, eastBelowY = 141, 72
            eastNearX, eastNearY = 122, 68
            eastAboveX, eastAboveY = 107, 66

            southEastBelowX, southEastBelowY = 127, 104
            southEastNearX, southEastNearY = 113, 89
            southEastAboveX, southEastAboveY = 102, 76

            southBelowX, southBelowY = 91, 117
            southNearX, southNearY = southBelowX, 97
            southAboveX, southAboveY = southBelowX, 77

            southWestBelowX, southWestBelowY = 56, 104
            southWestNearX, southWestNearY = 70, 88
            southWestAboveX, southWestAboveY = 81, 75

            westBelowX, westBelowY = 43, 73
            westNearX, westNearY = 63, 70 
            westAboveX, westAboveY = 77, 66 

            northWestBelowX, northWestBelowY = 56, 44
            northWestNearX, northWestNearY = 70, 52
            northWestAboveX, northWestAboveY = 82, 58

        #TODO: need to replace this with a global constant
        #im = Image.open('/Users/scottcha/Documents/ProblemAvyRoseImages' + '/thumbs/' + rose_file_image.replace('full', 'small'))
        im = Image.open('D:/Temp/ProblemAvyRoseImages' + '/thumbs/' + rose_file_image.replace('full', 'small'))

        rgb_im = im.convert('RGB')

        if(problem_type == 'Forecast'):
            item['Day1Danger_OctagonBelowTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northBelowX, northBelowY)))
            item['Day1Danger_OctagonNearTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northNearX, northNearY)))
            item['Day1Danger_OctagonAboveTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northAboveX, northAboveY)))
            item['Day1Danger_OctagonBelowTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastBelowX, northEastBelowY)))
            item['Day1Danger_OctagonNearTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastNearX, northEastNearY)))
            item['Day1Danger_OctagonAboveTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastAboveX, northEastAboveY)))
            item['Day1Danger_OctagonBelowTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastBelowX, eastBelowY)))
            item['Day1Danger_OctagonNearTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastNearX, eastNearY)))
            item['Day1Danger_OctagonAboveTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastAboveX, eastAboveY)))
            item['Day1Danger_OctagonBelowTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastBelowX, southEastBelowY)))
            item['Day1Danger_OctagonNearTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastNearX, southEastNearY)))
            item['Day1Danger_OctagonAboveTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastAboveX, southEastAboveY)))
            item['Day1Danger_OctagonBelowTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southBelowX, southBelowY)))
            item['Day1Danger_OctagonNearTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southNearX, southNearY)))
            item['Day1Danger_OctagonAboveTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southAboveX, southAboveY)))
            item['Day1Danger_OctagonBelowTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestBelowX, southWestBelowY)))
            item['Day1Danger_OctagonNearTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestNearX, southWestNearY)))
            item['Day1Danger_OctagonAboveTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestAboveX, southWestAboveY)))
            item['Day1Danger_OctagonBelowTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westBelowX, westBelowY)))
            item['Day1Danger_OctagonNearTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westNearX, westNearY)))
            item['Day1Danger_OctagonAboveTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westAboveX, westAboveY)))
            item['Day1Danger_OctagonBelowTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestBelowX, northWestBelowY)))
            item['Day1Danger_OctagonNearTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestNearX, northWestNearY)))
            item['Day1Danger_OctagonAboveTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestAboveX, northWestAboveY)))
        elif(problem_type == 'Cornices'):
            item['Cornices_OctagonBelowTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northBelowX, northBelowY)))
            item['Cornices_OctagonNearTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northNearX, northNearY)))
            item['Cornices_OctagonAboveTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northAboveX, northAboveY)))
            item['Cornices_OctagonBelowTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastBelowX, northEastBelowY)))
            item['Cornices_OctagonNearTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastNearX, northEastNearY)))
            item['Cornices_OctagonAboveTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastAboveX, northEastAboveY)))
            item['Cornices_OctagonBelowTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastBelowX, eastBelowY)))
            item['Cornices_OctagonNearTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastNearX, eastNearY)))
            item['Cornices_OctagonAboveTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastAboveX, eastAboveY)))
            item['Cornices_OctagonBelowTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastBelowX, southEastBelowY)))
            item['Cornices_OctagonNearTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastNearX, southEastNearY)))
            item['Cornices_OctagonAboveTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastAboveX, southEastAboveY)))
            item['Cornices_OctagonBelowTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southBelowX, southBelowY)))
            item['Cornices_OctagonNearTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southNearX, southNearY)))
            item['Cornices_OctagonAboveTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southAboveX, southAboveY)))
            item['Cornices_OctagonBelowTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestBelowX, southWestBelowY)))
            item['Cornices_OctagonNearTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestNearX, southWestNearY)))
            item['Cornices_OctagonAboveTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestAboveX, southWestAboveY)))
            item['Cornices_OctagonBelowTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westBelowX, westBelowY)))
            item['Cornices_OctagonNearTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westNearX, westNearY)))
            item['Cornices_OctagonAboveTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westAboveX, westAboveY)))
            item['Cornices_OctagonBelowTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestBelowX, northWestBelowY)))
            item['Cornices_OctagonNearTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestNearX, northWestNearY)))
            item['Cornices_OctagonAboveTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestAboveX, northWestAboveY)))
        elif(problem_type == 'Glide'):
            item['Glide_OctagonBelowTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northBelowX, northBelowY)))
            item['Glide_OctagonNearTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northNearX, northNearY)))
            item['Glide_OctagonAboveTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northAboveX, northAboveY)))
            item['Glide_OctagonBelowTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastBelowX, northEastBelowY)))
            item['Glide_OctagonNearTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastNearX, northEastNearY)))
            item['Glide_OctagonAboveTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastAboveX, northEastAboveY)))
            item['Glide_OctagonBelowTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastBelowX, eastBelowY)))
            item['Glide_OctagonNearTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastNearX, eastNearY)))
            item['Glide_OctagonAboveTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastAboveX, eastAboveY)))
            item['Glide_OctagonBelowTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastBelowX, southEastBelowY)))
            item['Glide_OctagonNearTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastNearX, southEastNearY)))
            item['Glide_OctagonAboveTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastAboveX, southEastAboveY)))
            item['Glide_OctagonBelowTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southBelowX, southBelowY)))
            item['Glide_OctagonNearTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southNearX, southNearY)))
            item['Glide_OctagonAboveTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southAboveX, southAboveY)))
            item['Glide_OctagonBelowTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestBelowX, southWestBelowY)))
            item['Glide_OctagonNearTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestNearX, southWestNearY)))
            item['Glide_OctagonAboveTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestAboveX, southWestAboveY)))
            item['Glide_OctagonBelowTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westBelowX, westBelowY)))
            item['Glide_OctagonNearTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westNearX, westNearY)))
            item['Glide_OctagonAboveTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westAboveX, westAboveY)))
            item['Glide_OctagonBelowTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestBelowX, northWestBelowY)))
            item['Glide_OctagonNearTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestNearX, northWestNearY)))
            item['Glide_OctagonAboveTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestAboveX, northWestAboveY)))
        elif(problem_type == 'LooseDry'):
            item['LooseDry_OctagonBelowTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northBelowX, northBelowY)))
            item['LooseDry_OctagonNearTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northNearX, northNearY)))
            item['LooseDry_OctagonAboveTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northAboveX, northAboveY)))
            item['LooseDry_OctagonBelowTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastBelowX, northEastBelowY)))
            item['LooseDry_OctagonNearTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastNearX, northEastNearY)))
            item['LooseDry_OctagonAboveTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastAboveX, northEastAboveY)))
            item['LooseDry_OctagonBelowTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastBelowX, eastBelowY)))
            item['LooseDry_OctagonNearTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastNearX, eastNearY)))
            item['LooseDry_OctagonAboveTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastAboveX, eastAboveY)))
            item['LooseDry_OctagonBelowTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastBelowX, southEastBelowY)))
            item['LooseDry_OctagonNearTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastNearX, southEastNearY)))
            item['LooseDry_OctagonAboveTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastAboveX, southEastAboveY)))
            item['LooseDry_OctagonBelowTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southBelowX, southBelowY)))
            item['LooseDry_OctagonNearTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southNearX, southNearY)))
            item['LooseDry_OctagonAboveTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southAboveX, southAboveY)))
            item['LooseDry_OctagonBelowTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestBelowX, southWestBelowY)))
            item['LooseDry_OctagonNearTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestNearX, southWestNearY)))
            item['LooseDry_OctagonAboveTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestAboveX, southWestAboveY)))
            item['LooseDry_OctagonBelowTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westBelowX, westBelowY)))
            item['LooseDry_OctagonNearTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westNearX, westNearY)))
            item['LooseDry_OctagonAboveTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westAboveX, westAboveY)))
            item['LooseDry_OctagonBelowTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestBelowX, northWestBelowY)))
            item['LooseDry_OctagonNearTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestNearX, northWestNearY)))
            item['LooseDry_OctagonAboveTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestAboveX, northWestAboveY)))
        elif(problem_type == 'LooseWet'):
            item['LooseWet_OctagonBelowTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northBelowX, northBelowY)))
            item['LooseWet_OctagonNearTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northNearX, northNearY)))
            item['LooseWet_OctagonAboveTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northAboveX, northAboveY)))
            item['LooseWet_OctagonBelowTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastBelowX, northEastBelowY)))
            item['LooseWet_OctagonNearTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastNearX, northEastNearY)))
            item['LooseWet_OctagonAboveTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastAboveX, northEastAboveY)))
            item['LooseWet_OctagonBelowTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastBelowX, eastBelowY)))
            item['LooseWet_OctagonNearTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastNearX, eastNearY)))
            item['LooseWet_OctagonAboveTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastAboveX, eastAboveY)))
            item['LooseWet_OctagonBelowTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastBelowX, southEastBelowY)))
            item['LooseWet_OctagonNearTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastNearX, southEastNearY)))
            item['LooseWet_OctagonAboveTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastAboveX, southEastAboveY)))
            item['LooseWet_OctagonBelowTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southBelowX, southBelowY)))
            item['LooseWet_OctagonNearTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southNearX, southNearY)))
            item['LooseWet_OctagonAboveTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southAboveX, southAboveY)))
            item['LooseWet_OctagonBelowTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestBelowX, southWestBelowY)))
            item['LooseWet_OctagonNearTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestNearX, southWestNearY)))
            item['LooseWet_OctagonAboveTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestAboveX, southWestAboveY)))
            item['LooseWet_OctagonBelowTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westBelowX, westBelowY)))
            item['LooseWet_OctagonNearTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westNearX, westNearY)))
            item['LooseWet_OctagonAboveTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westAboveX, westAboveY)))
            item['LooseWet_OctagonBelowTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestBelowX, northWestBelowY)))
            item['LooseWet_OctagonNearTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestNearX, northWestNearY)))
            item['LooseWet_OctagonAboveTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestAboveX, northWestAboveY)))
        elif(problem_type == 'PersistentSlab'):
            item['PersistentSlab_OctagonBelowTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northBelowX, northBelowY)))
            item['PersistentSlab_OctagonNearTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northNearX, northNearY)))
            item['PersistentSlab_OctagonAboveTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northAboveX, northAboveY)))
            item['PersistentSlab_OctagonBelowTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastBelowX, northEastBelowY)))
            item['PersistentSlab_OctagonNearTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastNearX, northEastNearY)))
            item['PersistentSlab_OctagonAboveTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastAboveX, northEastAboveY)))
            item['PersistentSlab_OctagonBelowTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastBelowX, eastBelowY)))
            item['PersistentSlab_OctagonNearTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastNearX, eastNearY)))
            item['PersistentSlab_OctagonAboveTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastAboveX, eastAboveY)))
            item['PersistentSlab_OctagonBelowTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastBelowX, southEastBelowY)))
            item['PersistentSlab_OctagonNearTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastNearX, southEastNearY)))
            item['PersistentSlab_OctagonAboveTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastAboveX, southEastAboveY)))
            item['PersistentSlab_OctagonBelowTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southBelowX, southBelowY)))
            item['PersistentSlab_OctagonNearTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southNearX, southNearY)))
            item['PersistentSlab_OctagonAboveTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southAboveX, southAboveY)))
            item['PersistentSlab_OctagonBelowTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestBelowX, southWestBelowY)))
            item['PersistentSlab_OctagonNearTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestNearX, southWestNearY)))
            item['PersistentSlab_OctagonAboveTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestAboveX, southWestAboveY)))
            item['PersistentSlab_OctagonBelowTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westBelowX, westBelowY)))
            item['PersistentSlab_OctagonNearTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westNearX, westNearY)))
            item['PersistentSlab_OctagonAboveTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westAboveX, westAboveY)))
            item['PersistentSlab_OctagonBelowTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestBelowX, northWestBelowY)))
            item['PersistentSlab_OctagonNearTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestNearX, northWestNearY)))
            item['PersistentSlab_OctagonAboveTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestAboveX, northWestAboveY)))
        elif(problem_type == 'DeepPersistentSlab'):
            item['DeepPersistentSlab_OctagonBelowTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northBelowX, northBelowY)))
            item['DeepPersistentSlab_OctagonNearTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northNearX, northNearY)))
            item['DeepPersistentSlab_OctagonAboveTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northAboveX, northAboveY)))
            item['DeepPersistentSlab_OctagonBelowTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastBelowX, northEastBelowY)))
            item['DeepPersistentSlab_OctagonNearTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastNearX, northEastNearY)))
            item['DeepPersistentSlab_OctagonAboveTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastAboveX, northEastAboveY)))
            item['DeepPersistentSlab_OctagonBelowTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastBelowX, eastBelowY)))
            item['DeepPersistentSlab_OctagonNearTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastNearX, eastNearY)))
            item['DeepPersistentSlab_OctagonAboveTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastAboveX, eastAboveY)))
            item['DeepPersistentSlab_OctagonBelowTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastBelowX, southEastBelowY)))
            item['DeepPersistentSlab_OctagonNearTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastNearX, southEastNearY)))
            item['DeepPersistentSlab_OctagonAboveTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastAboveX, southEastAboveY)))
            item['DeepPersistentSlab_OctagonBelowTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southBelowX, southBelowY)))
            item['DeepPersistentSlab_OctagonNearTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southNearX, southNearY)))
            item['DeepPersistentSlab_OctagonAboveTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southAboveX, southAboveY)))
            item['DeepPersistentSlab_OctagonBelowTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestBelowX, southWestBelowY)))
            item['DeepPersistentSlab_OctagonNearTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestNearX, southWestNearY)))
            item['DeepPersistentSlab_OctagonAboveTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestAboveX, southWestAboveY)))
            item['DeepPersistentSlab_OctagonBelowTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westBelowX, westBelowY)))
            item['DeepPersistentSlab_OctagonNearTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westNearX, westNearY)))
            item['DeepPersistentSlab_OctagonAboveTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westAboveX, westAboveY)))
            item['DeepPersistentSlab_OctagonBelowTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestBelowX, northWestBelowY)))
            item['DeepPersistentSlab_OctagonNearTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestNearX, northWestNearY)))
            item['DeepPersistentSlab_OctagonAboveTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestAboveX, northWestAboveY)))
        elif(problem_type == 'StormSlabs'):
            item['StormSlabs_OctagonBelowTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northBelowX, northBelowY)))
            item['StormSlabs_OctagonNearTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northNearX, northNearY)))
            item['StormSlabs_OctagonAboveTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northAboveX, northAboveY)))
            item['StormSlabs_OctagonBelowTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastBelowX, northEastBelowY)))
            item['StormSlabs_OctagonNearTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastNearX, northEastNearY)))
            item['StormSlabs_OctagonAboveTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastAboveX, northEastAboveY)))
            item['StormSlabs_OctagonBelowTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastBelowX, eastBelowY)))
            item['StormSlabs_OctagonNearTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastNearX, eastNearY)))
            item['StormSlabs_OctagonAboveTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastAboveX, eastAboveY)))
            item['StormSlabs_OctagonBelowTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastBelowX, southEastBelowY)))
            item['StormSlabs_OctagonNearTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastNearX, southEastNearY)))
            item['StormSlabs_OctagonAboveTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastAboveX, southEastAboveY)))
            item['StormSlabs_OctagonBelowTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southBelowX, southBelowY)))
            item['StormSlabs_OctagonNearTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southNearX, southNearY)))
            item['StormSlabs_OctagonAboveTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southAboveX, southAboveY)))
            item['StormSlabs_OctagonBelowTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestBelowX, southWestBelowY)))
            item['StormSlabs_OctagonNearTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestNearX, southWestNearY)))
            item['StormSlabs_OctagonAboveTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestAboveX, southWestAboveY)))
            item['StormSlabs_OctagonBelowTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westBelowX, westBelowY)))
            item['StormSlabs_OctagonNearTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westNearX, westNearY)))
            item['StormSlabs_OctagonAboveTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westAboveX, westAboveY)))
            item['StormSlabs_OctagonBelowTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestBelowX, northWestBelowY)))
            item['StormSlabs_OctagonNearTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestNearX, northWestNearY)))
            item['StormSlabs_OctagonAboveTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestAboveX, northWestAboveY)))
        elif(problem_type == 'WetSlabs'):
            item['WetSlabs_OctagonBelowTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northBelowX, northBelowY)))
            item['WetSlabs_OctagonNearTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northNearX, northNearY)))
            item['WetSlabs_OctagonAboveTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northAboveX, northAboveY)))
            item['WetSlabs_OctagonBelowTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastBelowX, northEastBelowY)))
            item['WetSlabs_OctagonNearTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastNearX, northEastNearY)))
            item['WetSlabs_OctagonAboveTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastAboveX, northEastAboveY)))
            item['WetSlabs_OctagonBelowTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastBelowX, eastBelowY)))
            item['WetSlabs_OctagonNearTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastNearX, eastNearY)))
            item['WetSlabs_OctagonAboveTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastAboveX, eastAboveY)))
            item['WetSlabs_OctagonBelowTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastBelowX, southEastBelowY)))
            item['WetSlabs_OctagonNearTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastNearX, southEastNearY)))
            item['WetSlabs_OctagonAboveTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastAboveX, southEastAboveY)))
            item['WetSlabs_OctagonBelowTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southBelowX, southBelowY)))
            item['WetSlabs_OctagonNearTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southNearX, southNearY)))
            item['WetSlabs_OctagonAboveTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southAboveX, southAboveY)))
            item['WetSlabs_OctagonBelowTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestBelowX, southWestBelowY)))
            item['WetSlabs_OctagonNearTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestNearX, southWestNearY)))
            item['WetSlabs_OctagonAboveTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestAboveX, southWestAboveY)))
            item['WetSlabs_OctagonBelowTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westBelowX, westBelowY)))
            item['WetSlabs_OctagonNearTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westNearX, westNearY)))
            item['WetSlabs_OctagonAboveTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westAboveX, westAboveY)))
            item['WetSlabs_OctagonBelowTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestBelowX, northWestBelowY)))
            item['WetSlabs_OctagonNearTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestNearX, northWestNearY)))
            item['WetSlabs_OctagonAboveTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestAboveX, northWestAboveY)))
        elif(problem_type == 'WindSlab'):
            item['WindSlab_OctagonBelowTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northBelowX, northBelowY)))
            item['WindSlab_OctagonNearTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northNearX, northNearY)))
            item['WindSlab_OctagonAboveTreelineNorth'] = self.rgb_to_danger(*rgb_im.getpixel((northAboveX, northAboveY)))
            item['WindSlab_OctagonBelowTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastBelowX, northEastBelowY)))
            item['WindSlab_OctagonNearTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastNearX, northEastNearY)))
            item['WindSlab_OctagonAboveTreelineNorthEast'] = self.rgb_to_danger(*rgb_im.getpixel((northEastAboveX, northEastAboveY)))
            item['WindSlab_OctagonBelowTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastBelowX, eastBelowY)))
            item['WindSlab_OctagonNearTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastNearX, eastNearY)))
            item['WindSlab_OctagonAboveTreelineEast'] = self.rgb_to_danger(*rgb_im.getpixel((eastAboveX, eastAboveY)))
            item['WindSlab_OctagonBelowTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastBelowX, southEastBelowY)))
            item['WindSlab_OctagonNearTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastNearX, southEastNearY)))
            item['WindSlab_OctagonAboveTreelineSouthEast'] = self.rgb_to_danger(*rgb_im.getpixel((southEastAboveX, southEastAboveY)))
            item['WindSlab_OctagonBelowTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southBelowX, southBelowY)))
            item['WindSlab_OctagonNearTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southNearX, southNearY)))
            item['WindSlab_OctagonAboveTreelineSouth'] = self.rgb_to_danger(*rgb_im.getpixel((southAboveX, southAboveY)))
            item['WindSlab_OctagonBelowTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestBelowX, southWestBelowY)))
            item['WindSlab_OctagonNearTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestNearX, southWestNearY)))
            item['WindSlab_OctagonAboveTreelineSouthWest'] = self.rgb_to_danger(*rgb_im.getpixel((southWestAboveX, southWestAboveY)))
            item['WindSlab_OctagonBelowTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westBelowX, westBelowY)))
            item['WindSlab_OctagonNearTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westNearX, westNearY)))
            item['WindSlab_OctagonAboveTreelineWest'] = self.rgb_to_danger(*rgb_im.getpixel((westAboveX, westAboveY)))
            item['WindSlab_OctagonBelowTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestBelowX, northWestBelowY)))
            item['WindSlab_OctagonNearTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestNearX, northWestNearY)))
            item['WindSlab_OctagonAboveTreelineNorthWest'] = self.rgb_to_danger(*rgb_im.getpixel((northWestAboveX, northWestAboveY)))
        else:
            sys.stderr.write("Incorrect problem_type: " + problem_type + " skipping")    

        return 

    def item_completed(self, results, item, info):
        print("In Item Completed")
        image_paths = [x['path'] for ok, x in results if ok]
        if not image_paths:
            raise DropItem("****Item contains no images")
        item['image_paths'] = image_paths
        images = item['image_paths']
        if(len(images)>0):
            print("Item contains images")

            for x in range(0, len(images)):
                print("attempting to parse image type " + item['image_types'][x] + ' with image path ' + images[x])
                self.interpret_rose_graphic(images[x], item, item['image_types'][x])
        else:
            print("Item contains no images") 

        return item


