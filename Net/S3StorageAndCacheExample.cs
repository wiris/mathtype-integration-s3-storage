using System;
using System.Collections.Generic;

using com.wiris.system;

namespace com.wiris.plugin.storage {

    // S3
    using Amazon;
    using Amazon.S3;
    using Amazon.S3.Model;
    using com.wiris.util.sys;

    /**
    * This sample demonstrate how to implement a cache class for storing and retrieving WIRIS cache from Amazon Simple Storage Service (S3).
    * This class implements the com.wiris.plugin.storage.StorageAndCacheinterface. See http://www.wiris.com/pluginwiris_engine/docs/api for a description of the interface.
    *
    * <b>Prerequisites:</b> You must have a valid Amazon Web Services account, and be signed up to use Amazon S3. For more information on Amazon
    * S3, see http://aws.amazon.com/s3.
    * Fill in your AWS access credentials in the provided credentials file
    * template, and be sure to move the file to the default location
    * (~/.aws/credentials) where the sample code will load the credentials from.
    * <p>
    * <b>WARNING:</b> To avoid accidental leakage of your credentials, DO NOT keep
    * the credentials file in your source directory.
    *
    * http://aws.amazon.com/security-credentials
    *
    */

    public class S3StorageAndCacheExample : StorageAndCache
    {
        // Cache and Formula folders.
        const string CACHE_FOLDER = "cache";
        const string FORMULA_FOLDER = "formula";
        // S3 Bucket name.
        const string BUCKET_NAME = "@BUCKET_NAME@";
        // Amazon S3 Client.
        static private IAmazonS3 s3Client;
        // S3 Region endpoint.
        private RegionEndpoint region = RegionEndpoint.EUWest1;

        // Initializes the storage and cache system. This method is called before any call to other methods.
        public void init(object obj, Dictionary<string, string> config, Cache cache, Cache cacheFormula)
        {
            if (s3Client == null)
            {
                //Set the Region Endpoint http://docs.aws.amazon.com/general/latest/gr/rande.html#s3_region
                s3Client = new AmazonS3Client(region);
            }
        }

        /**
        * Given a content, computes a digest of it. This digest must be unique in order to use it as identifier of the content.
        * For example, the digest can be the md5 sum of the content.
        *
        * @param content
        * @return A computed digest of the content.
        */
        public string codeDigest(string content)
        {
            // Using WIRIS Std utils to encode the content to MD5.
            // It is strongly recommended to use MD5 class to generate the md5 string to avoid consistence issues.
            string digest = Md5Tools.encodeString(content);
            string key = FORMULA_FOLDER + "/" + getFolder(digest) + digest + "." + "ini";
            // Setup request for putting an object in S3.
            // See http://docs.aws.amazon.com/sdkfornet1/latest/apidocs/html/T_Amazon_S3_Model_PutObjectRequest.htm for more information.
            PutObjectRequest request = new PutObjectRequest
            {
                BucketName = BUCKET_NAME,
                Key = key,
                ContentBody = content,
                ContentType = "text/plain"
            };

            // Make service call and get back the response.
            PutObjectResponse response = s3Client.PutObject(request);
            return digest;
        }

        /**
        * Given a computed digest, returns the respective content.
        * You might need to store the relation digest content during the codeDigest.
        *
        * @param digest A computed digest.
        * @return The content associated to the computed digest. If it is not found, this method should return null.
        */
        public string decodeDigest(string digest)
        {
            string key = FORMULA_FOLDER + "/" + getFolder(digest) + digest + "." + "ini";
            try
            {
                // Retrieve an object from S3.
                // See http://docs.aws.amazon.com/sdkfornet1/latest/apidocs/html/T_Amazon_S3_Model_GetObjectResponse.htm for more information.
                using (GetObjectResponse getObjectResponse = s3Client.GetObject(BUCKET_NAME, key))
                {
                    using (System.IO.StreamReader reader = new System.IO.StreamReader(getObjectResponse.ResponseStream))
                    {
                        string contents = reader.ReadToEnd();
                        return contents;
                    }
                }
            }
            catch (Exception e)
            {
                return null; // Cache miss.
            }
        }

        /**
        * Given a computed digest, returns the stored data associated with it.
        * This should be a cache system. As a cache there is a contract between the implementation and the caller:
        * If the data is not found, the caller is responsible to regenerate and store the data.
        * The cache is free to remove any data.
        *
        * @param digest A computed digest.
        * @param service The service that request the data
        * @return The data associated with the digest. If it is not found, this method should return null.
        */
        public byte[] retreiveData(string digest, string service)
        {
            string key = CACHE_FOLDER + "/" + getFolder(digest) + digest + "." + getExtension(service);
            try
            {
                using (GetObjectResponse getObjectResponse = s3Client.GetObject(BUCKET_NAME, key))
                {
                    System.IO.MemoryStream ms = new System.IO.MemoryStream();
                    getObjectResponse.ResponseStream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
            catch (Exception e)
            {
                return null; // Cache miss.
            }
        }

        /**
        * Associates a data stream with a computed digest. Note that data are not shared by different service. Thus,
        * the pair digest and service must be the "primary key" of the data.
        * @param  $digest  A computed digest.
        * @param  $service The service that stores data.
        * @param  $stream  The data to be stored.
        */
        public void storeData(string digest, string service, byte[] stream)
        {
            string key = CACHE_FOLDER + "/" + getFolder(digest) + digest + "." + getExtension(service);

            PutObjectRequest request = new PutObjectRequest
            {
                BucketName = BUCKET_NAME,
                Key = key,
                ContentType = getExtension(service)
            };
            System.IO.MemoryStream inputStream = new System.IO.MemoryStream(stream);
            request.InputStream = inputStream;

            PutObjectResponse response = s3Client.PutObject(request);
        }

        /**
        * Deletes ALL the bucket objects. BE CAREFUL.
        */
        public void deleteCache()
        {
            try
            {
                ListObjectsV2Request request = new ListObjectsV2Request
                {
                    BucketName = BUCKET_NAME
                };
                ListObjectsV2Response response;
                do
                {
                    response = s3Client.ListObjectsV2(request);

                    // Process response.
                    foreach (S3Object entry in response.S3Objects)
                    {
                        s3Client.DeleteObject(BUCKET_NAME, entry.Key);
                    }
                    request.ContinuationToken = response.NextContinuationToken;
                } while (response.IsTruncated == true);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                    ||
                    amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    Console.WriteLine("Check the provided AWS Credentials.");
                    Console.WriteLine(
                    "To sign up for service, go to http://aws.amazon.com/s3");
                }
                else
                {
                    Console.WriteLine(
                        "Error occurred. Message:'{0}' when listing objects",
                        amazonS3Exception.Message);
                }
            }

        }

        /**
        * Given a service like png or svg, returns the associated extension.
        * @param service
        * @return The associated file extension. If the service is not an image servive (svg or png) returns "txt"
        * file extension.
        */
        private string getExtension(string service)
        {
            if (service.Equals("png")) return "png";
            if (service.Equals("svg")) return "svg";
            return service + ".txt";
        }

        /**
        * Given a MD5 digest returns the associated folder structure.
        * @param digest
        * @return Associated folder structure.
        */
        private string getFolder(string digest)
        {
            return digest.Substring(0, 2) + "/" + digest.Substring(2, 2);
        }

        /**
        * Given a service like png, svg returns the associated Content-Type HTTP header.
        * @param service
        * @return The associated HTTP header. If the service is not an image service (svg or png) returns "text/plain" header.
        */
        private String getExtensionEncoding(string service)
        {
            if (service.Equals("png"))
                return "image/png";
            if (service.Equals("svg"))
                return "image/svg+xml";
            return "text/plain";
        }
    }
}