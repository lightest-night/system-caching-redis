# Lightest Night
## Caching > Redis

Hooks, utilities and helpers to allow caching to the Redis distributed cache

#### How To Use
##### Registration
* Asp.Net Standard/Core Dependency Injection
  * Use the provided `services.AddRedisCache(Action<ConfigurationOptions> options)` method
  
* Other Containers
  * Register `Cache` as a Transient object.
    * You can use the `RedisConnectionFactory` as a way of deriving a Redis Connection Object for the `Cache` constructor

##### Usage
* `Task Save<TItem>(object key, TItem objectToSave, TimeSpan? expiry)`
  * An asynchronous function to call when saving an item to the Redis cache
  * **NB** expiry is optional, if not provided, cached items will not expire
  
* `Task<TItem> Get(object key)`
  * An asynchronous function to call when retrieving an item from the Redis cache
