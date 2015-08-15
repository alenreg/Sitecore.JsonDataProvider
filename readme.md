# Sitecore.JsonDataProvider

* JSON Data Provider allows storing parts of content tree in separate `*.json` files. 
* Unlike serialization, items exist only in `*.json` files which makes synchronization unnecessary. 
* Single `*.json` file stores all descendant items of the specific item that exists in a SQL database. 

#### Release Notes:

##### Sitecore.JsonDataProvider 1.0.0.0

Initial community preview release, for testing purpose only.  
List of tested functions: 
* CRUD JSON items
* CRUD JSON item versions
* Duplicate JSON item (and copy within same `.json` file area)
* CRUD JSON Templates