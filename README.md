# RedisHelper
This is a redis operation utility based on StackExchange.Redis.It's built for.Net Standard 2.0.

**[Nuget](https://www.nuget.org/packages/ColinChang.RedisHelper/)**
```sh
# Package Manager
Install-Package ColinChang.RedisHelper

# .NET CLI
dotnet add package ColinChang.RedisHelper
```
**Supported**

* Data types:`String,List,Set,SortedSet,Hash`
* Pub/Sub
* Path commands
* Distrubution lock(sync/async)

**Tips**
we highly recommend that use this as a singleton instance to reuse the redis connection.
