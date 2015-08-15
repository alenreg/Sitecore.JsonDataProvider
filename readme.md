# Sitecore.JsonDataProvider

* JSON Data Provider allows storing parts of content tree in separate `*.json` files. 
* Unlike serialization, items exist only in `*.json` files which makes synchronization unnecessary. 
* Single `*.json` file stores all descendant items of the specific item that exists in a SQL database. 

#### [Download](https://github.com/Sitecore/Sitecore.JsonDataProvider/releases/download/1.0.0.0/Sitecore.JsonDataProvider.zip)

#### Installation

* download the [Sitecore.JsonDataProvider.zip](https://github.com/Sitecore/Sitecore.JsonDataProvider/releases/download/1.0.0.0/Sitecore.JsonDataProvider.zip) archive file,
* unpack it to the `Website` folder

#### How to

* install a module (see above)
* modify `/sitecore/content/json demo` item in Content Editor or Experience Explorer
* find your changes in the `/App_Data/content-items.json` file
* look at `/App_Config/Include/Sitecore.JsonDataProvider.*.config` files to understand how it is bound together

#### Release Notes:

##### Sitecore.JsonDataProvider 1.0.0.0

Initial community preview release, for testing purpose only. Tested with Sitecore 8.0 Update-4.
List of tested functions: 
* CRUD JSON items
* CRUD JSON item versions
* Duplicate JSON item (and copy within same `.json` file area)
* CRUD JSON Templates
