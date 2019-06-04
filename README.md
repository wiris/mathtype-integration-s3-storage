MathType storage classes for Amazon Web Services S3
==========

# Custom MathType storage classes

MathType integrations need to store two pieces of information in the server. By default, this information is stored in the filesystem:

* The inverse association \<digest\> to MathML. This data cannot be removed unless it exists a mechanism able to regenerate them.
* A cache with the images generated from MathML. This cache can be removed if necessary and it will be regenerated when formulas are displayed.

In order to use Amazon S3 for storing and retrievingMathTypecache it is necessary to implement `com.wiris.plugin.storage.StorageAndCacheinterface` interface. See [server-side API](http://www.wiris.com/pluginwiris_engine/docs/api/) for a description of the interface.

# How to use the examples

The following examples show how to implement a S3 Storage class for *_Java_*, *_PHP_* and *_.Net_* . These sample classes al fully functional.

The following steps should be followed in order to run them. However, put hardcoded credentials is a bad practice, here are used to simplify the example. You can visit the links to [PHP](https://docs.aws.amazon.com/sdk-for-php/v3/developer-guide/guide_credentials.html),
[Java](https://docs.aws.amazon.com/sdk-for-java/v1/developer-guide/credentials.html) and
[.Net](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-config-creds.html) to learn how to do it well.

1. First of all, change the _BUCKET\_NAME_ value to the name of your bucket.
2. Then, insert your credential keys (i.e: _ACCESS\_KEY_ and _SECRET\_KEY_ values).
4. Next, change the _REGION_ value.
5. Finally, change the key `wirisstorageclass` of your `configuration.ini` to point to the S3 cache class. See [the formula persistence section](https://docs.wiris.com/en/mathtype/mathtype_web/integrations/formula-persistence) for more information.