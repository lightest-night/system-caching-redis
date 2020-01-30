# Lightest Night
## Caching > Redis

Hooks, utilities and helpers to allow caching to the Redis distributed cache

#### How To Use
##### Registration
* Asp.Net Standard/Core Dependency Injection
  * Use the provided `services.AddRedisCache(Action<RedisConnectionManager> options)` method
  
* Other Containers
  * Register an instance of `IRedisCacheProvider` as a Singleton
  * Register an instance of `ICache` as a Singleton

##### Usage
* `Task Save<TItem>(object key, TItem objectToSave, DateTime? expires, params string[] tags)`
  * An asynchronous function to call when saving an item to the Redis cache
  * **NB** expiry is optional, if not provided, cached items will not expire
  
* `Task<TItem> Get(object key)`
  * An asynchronous function to call when retrieving an item from the Redis cache
  
* `Task<IEnumerable<TItem>> GetByTag<TItem>(string tag)`
  * An asynchronous function to call when retrieving items by tag from the Redis cache
  
* `Task Delete<TItem>(object key)`
  * An asynchronous function to call when deleting an item from the cache  
  
###### Manual Expiry
If the instance of Redis in use doesn't support Key Events, then we can manually remove expired tags. To do this set the `ManuallyProcessExpiredKeys` option to `true` in the `CacheConfig`

Changed for stus benefit