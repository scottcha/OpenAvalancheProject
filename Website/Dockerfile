# Use an official Apache runtime as a parent image
FROM httpd:2.4-alpine

# Copy the static website files from the 'Website' directory to the Apache HTML directory
COPY ./Website /usr/local/apache2/htdocs/

# Apache server is already set to listen on port 80