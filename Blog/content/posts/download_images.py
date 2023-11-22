import os
import re
import requests

# Path to the markdown file
markdown_file = "index.md"

# Directory to save images
directory = "C:/Users/scott/source/repos/OpenAvalancheProject_Website/Blog/content/posts/20190620_a-global-avalanche-region-map/"

#combine directory and file with path.join
markdown_file = os.path.join(directory, markdown_file)

# Read the markdown text from the file
with open(markdown_file, 'r') as f:
    markdown_text = f.read()

# Find all image URLs
image_urls = re.findall(r'\((https://oapstorageprod.blob.core.windows.net/blog-images/[^)]+)\)', markdown_text)

# Create directory if it doesn't exist
os.makedirs(directory, exist_ok=True)

# Download each image
for url in image_urls:
    # Get the image name by splitting the URL
    image_name = url.split("/")[-1]
    
    # Download the image
    response = requests.get(url)
    
    # Save the image
    with open(os.path.join(directory, image_name), 'wb') as f:
        f.write(response.content)