API MOCKER
---
The project tries to help developers, by providing ability to mock services.  
Source location: https://github.com/Indomitable/api-mocker  
Before starting an environment variable `API_MOCKER_CONFIG` is needed with the path to the root configuration file.
Using docker image:
 1. Download image `docker pull indomitable/api-mocker:latest`
 2. Run container: Example map local port 9090 to cointainers 8080, volume mount of the configuration to config folder in the service home and set `API_MOCKER_CONFIG` variable
  `docker run --rm -p 9090:8080 -v ./example-config:/home/service/config  -e "API_MOCKER_CONFIG=/home/service/config/config.yaml" indomitable/api-mocker:latest`
 3. Test: `curl http://localhost:9090/countries` 
or use the docker compose file with `docker compose up`

The service support hot configuraiton load: It watches config files for changes and reloads the configuration on change.   
It has simple configuration in yaml format: 

 ```yaml
 server:
  url: http://localhost:5020       # set the url address when the service to listen. This is ignored for the docker image where port is fixed to 8080
  headers:
    Content-Type: application/json # global response headers, valid for all responses
    collections:                     # the responses are grouped in collections
    - path: /countries             # specify the base path of the collection. 
    headers:                     # set responses headers for all requests in this collection
    x-header: value            # collection headers override server headers
    requests:
    - method: GET              # define response for GET request. When no path is defined it is same as collection's path
    statusCode: 404          # response status code
    file: ./countries.json   # get set body from file

    - path: austria            # define response for POST request for path `/countries/austria`. 
    method: POST               
    statusCode: 200
    body: '{ "name": "Austria" }' # define body inline.

    - path: (?<name>[^/]+)     # can define the path as regex if named groups defined can be used as variables in the response  
      method: PUT
      statusCode: 401
      headers:                 # request specific headers, they override collection and server headers with same name
      x-message: test
      Content-Type: text/plain
      body: |                  # can have multiline bodies with variables taken from the url
      { 
        "name": "${name}"
      }
      - path: /absolute-path     # if request path starts with `/` then it ignores the collection path.        
      # request properties are optional. Default values:
      # method: GET
      # headers: empty value
      # statusCode: 200
      # body: empty body.
      # so if we call GET http://localhost:5020/absolute-path response will be:
      # statusCode = 200, body will be empty, it will take the headers from the collection and server.
      collections:
      - path: (?<countryName>[^/]+)/cities
        requests:
        - method: GET
        statusCode: 200
        file: ./countries/${countryName}/cities.json # variables can be used in the file paths.
        - include: ./people-collection.yaml                    # can spread configuration in multiple files.
```          

file: `.people-collection.yaml`
```yaml
        collection:                                         # define new collection with root path `/people.
        path: /people
        requests:
        - method: POST
        statusCode: 201
        body: |
        { "created": true }
```
        