import scrapy
import re
from items import AvyScrapingItem, AvyProblemItem

class UAC_Spider(scrapy.Spider):
    name = "uac"
    allowed_domains = ["utahavalanchecenter.org"]

    def start_requests(self):
        start_urls = [
            "https://utahavalanchecenter.org/sites/default/files/archive/advisory/print/advisory/salt-lake/20180408.html",
            "https://utahavalanchecenter.org/sites/default/files/archive/advisory/print/advisory/salt-lake/20180117.html"
        ]
        for url in start_urls:
                yield scrapy.Request(url=url, callback=self.parse)

    def cleanNewlinesAndStrip(self, stringToClean):
        stringToClean = stringToClean.replace('\n', '')
        stringToClean = stringToClean.replace('- ', '')
        stringToClean = stringToClean.strip()
        return stringToClean

    def processLikelihoodCode(self, code):
        if(code==1):
            return 'Unlikely'
        elif(code==2):
            return 'Possible'
        elif(code==3):
            return 'Likely'
        elif(code==4):
            return 'Very Likely'
        elif(code==5):
            return 'Certain'
        else:
            return 'Unknown Value'

    def processSize(self, code):
        if(code==1):
            return 'Small'
        elif(code==2):
            return 'Large'
        elif(code==3):
            return 'Very Large'
        elif(code==4 or code==5):
            return 'Historic'
        else:
            return 'Unknown value'

    def parse(self, response):
        item = AvyScrapingItem()
        item['forecastUrl'] = response.url

        #region
        item['regionName'] = response.xpath('//*[@id="subtitle-date-row"]/table/tbody/tr/th/text()').extract()

        #date/time
        tempDateTime = response.xpath('//*[@id="subtitle-date-row"]/table/tbody/tr/td/text()').extract()[0]

        #get just the date from somethign like this: Issued by Toby Weed for Thursday - January 12, 2017 - 6:52am
        tempDateTime = re.sub(r'Issued[\w\s]*\- ', '')
        tempDateTime = self.cleanNewlinesAndStrip(tempDateTime)
        item['dateTime'] = tempDateTime

        #TODO: 1. test the above
        #2. process overall danger image (how to account for different forecast types between nwac and utah?)
        #3. process problem types

        #above treeline day 1 & 2
        #item['day1DangerAboveTreeline'] = response.xpath('//*[@id="treeline-above"]/div[2]/div[2]/h4/text()').extract()
        #item['day2DangerAboveTreeline'] = response.xpath('//*[@id="treeline-above"]/div[3]/div[2]/h4/text()').extract()

        #near treeline day 1 & 2
        #item['day1DangerNearTreeline'] = response.xpath('//*[@id="treeline-near"]/div[2]/div[2]/h4/text()').extract()
        #item['day2DangerNearTreeline'] = response.xpath('//*[@id="treeline-near"]/div[3]/div[2]/h4/text()').extract()

        #below treeline day 1 & 2
        #item['day1DangerBelowTreeline'] = response.xpath('//*[@id="treeline-below"]/div[2]/div[2]/h4/text()').extract()
        #item['day2DangerBelowTreeline'] = response.xpath('//*[@id="treeline-below"]/div[3]/div[2]/h4/text()').extract()

        item['image_urls'] = []
        for x in range (0, 3):
            tempProblem = response.xpath('//*[@id="problems"]/div[' + str(x+2) + ']/div/div[1]/h3/text()').extract()
            if(len(tempProblem) > 0):
                tempProblem = self.cleanNewlinesAndStrip(tempProblem[0])
                #rose for problem1
                problem = AvyProblemItem()
                problem['problemType'] = tempProblem
                item['image_urls'].append("http://www.nwac.us" + response.xpath('//*[@id="problems"]/div['+ str(x+2) +']/div/ul/li[2]/div/img/@src').extract()[0])
                #likelihood for problem 1
                problem['likelihood'] = self.processLikelihoodCode(int(response.xpath('//*[@id="problems"]/div[' + str(x+2) + ']/div/ul/li[3]/div/img/@src').re('-\d(\d).png')[0]))
                problem['size'] = self.processSize(int(response.xpath('//*[@id="problems"]/div[' + str(x+2) + ']/div/ul/li[4]/div/img/@src').re('-\d(\d).png')[0]))
                if(x == 0):
                    item['problem1Type'] = problem['problemType']
                    item['problem1Likelihood'] = problem['likelihood']
                    item['problem1Size'] = problem['size']
                elif(x==1):
                    item['problem2Type'] = problem['problemType']
                    item['problem2Likelihood'] = problem['likelihood']
                    item['problem2Size'] = problem['size']
                elif(x==2):
                    item['problem3Type'] = problem['problemType']
                    item['problem3Likelihood'] = problem['likelihood']
                    item['problem3Size'] = problem['size']



     

        yield item







