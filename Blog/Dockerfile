# Start from the Ubuntu image and name this stage as 'builder'
# Note: alpine linux isn't compatible with the hugo command (without hacks around glibc)
FROM ubuntu:latest AS builder

# Install Hugo and Git
RUN apt-get update && \
    apt-get install -y wget ca-certificates git && \
    wget https://github.com/gohugoio/hugo/releases/download/v0.120.4/hugo_extended_0.120.4_Linux-64bit.tar.gz && \
    tar xzf hugo_extended_0.120.4_Linux-64bit.tar.gz && \
    mv hugo /usr/local/bin/ && \
    rm -r hugo_extended_0.120.4_Linux-64bit.tar.gz

# Copy your Hugo site source into the Docker container
COPY . /src

# Set the working directory
WORKDIR /src

# Initialize Git and update the LoveIt theme submodule
RUN git init && \
    git submodule update --init --recursive

WORKDIR /src/Blog 

# Build your Hugo site
RUN hugo --minify --config hugo.toml

# Use an Apache HTTP Server Docker image to serve your Hugo site
FROM httpd:2.4-alpine

# Copy the built Hugo site from the builder container to the Apache HTTP Server container
COPY --from=builder /src/Blog/public/ /usr/local/apache2/htdocs/