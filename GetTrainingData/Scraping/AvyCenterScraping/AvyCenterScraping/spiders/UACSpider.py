import scrapy
import re
import sys
import datetime
from AvyCenterScraping.items import AvyScrapingItem
import AvyCenterScraping.spiders.UACUrls as u

class UAC_Spider(scrapy.Spider):
    name = "uac"
    allowed_domains = ["utahavalanchecenter.org"]
    debug = False 
    
    def start_requests(self):
        if(self.debug):
            start_urls = [
                # "/sites/default/files/archive/advisory/print/advisory/salt-lake/20180408.html",
                # "/sites/default/files/archive/advisory/print/advisory/salt-lake/20180117.html",
                # "/sites/default/files/archive/advisory/print/advisory/salt-lake/20170108.html",
                # "/sites/default/files/archive/advisory/print/advisory/salt-lake/20170109.html",
                # "/sites/default/files/archive/advisory/print/advisory/salt-lake/20140209.html",
                # "/sites/default/files/archive/advisory/print/advisory/salt-lake/20150310.html",
                # "/sites/default/files/archive/advisory/print/advisory/salt-lake/20170310.html"
                "/sites/default/files/archive/advisory/print/advisory/ogden/20150204.html",
                "/sites/default/files/archive/advisory/print/advisory/ogden/20140408.html",
                "/sites/default/files/archive/advisory/print/advisory/ogden/20131228.html"

            ]
        else:
            start_urls = u.get_start_urls()['Abajo']
            # start_urls.append(u.get_start_urls()['Skyline'])
            # start_urls.append(u.get_start_urls()['Moab'])
            # start_urls.append(u.get_start_urls()['Abajo'])

        for url in start_urls:
            url = "https://utahavalanchecenter.org" + url
            print("calling url " + url)
            yield scrapy.Request(url=url, callback=self.parse)

    def cleanNewlinesAndStrip(self, stringToClean):
        stringToClean = stringToClean.replace('\n', '')
        stringToClean = stringToClean.replace('- ', '')
        stringToClean = stringToClean.strip()
        return stringToClean

    def process_likelihood_code(self, code):
        print("in process_likelihood with code: " + code)
        if(code == '0'):
            return 'no-data'
        elif(code=='1'):
            return '0-unlikely'
        elif(code=='2'):
            return '1-possible'
        elif(code=='3'):
            return '2-likely'
        elif(code=='4'):
            return '3-very likely'
        elif(code=='5'):
            return '4-certain'
        else:
            return 'Unknown Value'

    def process_size(self, code):
        print("in process_size with code: " + str(code))
        if(code == '0'):
            return 'no-data'
        if(code=='1'):
            return '0-small'
        elif(code=='2'):
            return '1-large'
        elif(code=='3'):
            return '2-very large'
        elif(code=='4' or code=='5'):
            return '3-historic'
        else:
            return 'Unknown value'

    def process_problem_type(self, code):
        print("in process_problem_type with code: " + code)
        if(code == 'wet-slab'): #checked
            return 'WetSlabs'
        elif(code == 'glide'): #checked
            return 'Glide'
        elif(code == 'storm-slab'): #checked
            return 'StormSlabs'
        elif(code == 'deep-slab'): #checked
            return 'DeepPersistentSlab'
        elif(code == 'wind-slab'): #checked
            return 'WindSlab'
        elif(code == 'wet-avalanche'): #checked
            return 'LooseWet'
        elif(code == 'loose-wet-snow'): #checked
            return 'LooseWet'
        elif(code == 'persistent-slab'): #checked
            return 'PersistentSlab'
        elif(code == 'loose-dry-snow'):
            return 'LooseDry'
        elif(code == 'loose-snow'): #checked
            return 'LooseDry'
        elif(code == 'cornice-fall'): #checked
            return 'Cornices'
        elif(code == 'normal-caution'): #checked
            return ''
        else:
            return 'Unknown avalanche type with code: ' + code

    def set_problem(self, item, problem_type, likelihood, size):
        print("in set_problem with problem_type: " + problem_type) 
        if(problem_type == 'Cornices'):
            item['Cornices_Likelihood'] = likelihood
            item['Cornices_MaximumSize'] = size
            item['Cornices_MinimumSize'] = size  #UAC only provides one size forecast
        elif(problem_type == 'Glide'):
            item['Glide_Likelihood'] = likelihood
            item['Glide_MaximumSize'] = size
            item['Glide_MinimumSize'] = size  #UAC only provides one size forecast
        elif(problem_type == 'LooseDry'):
            item['LooseDry_Likelihood'] = likelihood
            item['LooseDry_MaximumSize'] = size
            item['LooseDry_MinimumSize'] = size  #UAC only provides one size forecast
        elif(problem_type == 'LooseWet'):
            item['LooseWet_Likelihood'] = likelihood
            item['LooseWet_MaximumSize'] = size
            item['LooseWet_MinimumSize'] = size  #UAC only provides one size forecast
        elif(problem_type == 'PersistentSlab'):
            item['PersistentSlab_Likelihood'] = likelihood
            item['PersistentSlab_MaximumSize'] = size
            item['PersistentSlab_MinimumSize'] = size  #UAC only provides one size forecast
        elif(problem_type == 'DeepPersistentSlab'):
            item['DeepPersistentSlab_Likelihood'] = likelihood
            item['DeepPersistentSlab_MaximumSize'] = size
            item['DeepPersistentSlab_MinimumSize'] = size  #UAC only provides one size forecast
        elif(problem_type == 'StormSlabs'):
            item['StormSlabs_Likelihood'] = likelihood
            item['StormSlabs_MaximumSize'] = size
            item['StormSlabs_MinimumSize'] = size  #UAC only provides one size forecast
        elif(problem_type == 'WetSlabs'):
            item['WetSlabs_Likelihood'] = likelihood
            item['WetSlabs_MaximumSize'] = size
            item['WetSlabs_MinimumSize'] = size  #UAC only provides one size forecast
        elif(problem_type == 'WindSlab'):
            item['WindSlab_Likelihood'] = likelihood
            item['WindSlab_MaximumSize'] = size
            item['WindSlab_MinimumSize'] = size  #UAC only provides one size forecast
        else:
            sys.stderr.write("Incorrect problem_type in set_problem: " + problem_type + " skipping")    

    def parse(self, response):

        item = AvyScrapingItem()
        
        #set default values
        for field in item.fields:
            if('Octagon' in field or 'Likelihood' in field or 'MaximumSize' in field or 'MinimumSize' in field):
                item.setdefault(field, 'no-data')

        item['ForecastUrl'] = response.url

        #region
        item['Region'] = self.cleanNewlinesAndStrip(response.xpath('//*[@id="subtitle-date-row"]/table/tr/th/text()').extract()[0].split(':')[1])

        #date/time
        tempDateTime = response.xpath('//*[@id="subtitle-date-row"]/table/tr/td/text()').extract()[0]

        #get just the date from something like this: Issued by Toby Weed for Thursday - January 12, 2017 - 6:52am
        tempDateTime = re.sub(r'Issued[\w\s]*\- ', '', tempDateTime)
        tempDateTime = tempDateTime.replace("-", "")
        tempDateTime = self.cleanNewlinesAndStrip(tempDateTime)
        tempDateTime = datetime.datetime.strptime(tempDateTime, '%B %d, %Y %I:%M%p')
        item['PublishedDateTime'] = tempDateTime.strftime("%Y%m%d %H:00")
        item['Day1Date'] = item['PublishedDateTime'] 

        item['BottomLineSummary'] = self.cleanNewlinesAndStrip(response.xpath('//*[@id="problem-rose"]/table/tr/td[2]').extract()[0].replace(',', '')) #remove , so it can export to csv

        warning = response.xpath('//*[@id="warning-row"]/table/tr/td[2]/p').extract()
        if(len(warning) > 0):
            item['Day1Warning'] = 'Warning'
            item['Day1WarningEnd'] = 'no-data'
            item['Day1WarningText'] = self.cleanNewlinesAndStrip(warning[0].replace(',', ''))

        watch = response.xpath('//*[@id="watch-row"]/table/tr/td[2]/p').extract()
        if(len(watch) > 0):
            item['Day1Warning'] = 'Watch'
            item['Day1WarningEnd'] = 'no-data'
            item['Day1WarningText'] = self.cleanNewlinesAndStrip(watch[0].replace(',', ''))

        item['image_urls'] = []

        avy_rose = response.xpath('//*[@id="problem-rose"]/table/tr/td[1]/img/@src').extract()[0]
        item['image_urls'].append(avy_rose.replace('../../', 'https://utahavalanchecenter.org/'))
        item['image_types'] = []
        item['image_types'].append("Forecast")

        #find # of avy problems (max 3)
        avy_problems = response.xpath('//*[@id="problem-type"]/a/@href').extract()
        problem_roses = response.xpath('//*[@id="problem-rose"]/img/@src').extract()
        problem_likelihoods = response.xpath('//*[@id="characteristic-wrapper"]/div[1]/div[2]/img/@src').extract()
        problem_sizes = response.xpath('//*[@id="characteristic-wrapper"]/div[2]/div[2]/img/@src').extract()
        print("have " + str(len(avy_problems)) + " problems to parse")
        for x in range(0, len(avy_problems)):
            #get last element of url describing problem
            problem_type = self.process_problem_type(avy_problems[x].split('/')[-1])
            #split the url, get last element, split the .gif off, split on - and get the number code
            problem_likelihood = self.process_likelihood_code(problem_likelihoods[x].split('/')[-1].split('.')[0].split('-')[-1])
            problem_size = self.process_size(problem_sizes[x].split('/')[-1].split('.')[0].split('-')[-1])
            item['image_urls'].append(problem_roses[x].replace('../../', 'https://utahavalanchecenter.org/'))
            item['image_types'].append(problem_type)
            self.set_problem(item, problem_type, problem_likelihood, problem_size)

        yield item







