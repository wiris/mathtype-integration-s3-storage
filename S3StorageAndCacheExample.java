package com.wiris.plugin.storage;

import com.amazonaws.auth.AWSCredentials;
import com.amazonaws.auth.BasicAWSCredentials;
import com.amazonaws.auth.InstanceProfileCredentialsProvider;
import com.amazonaws.regions.Region;
import com.amazonaws.regions.Regions;
import com.amazonaws.services.s3.AmazonS3Client;
import com.amazonaws.services.s3.model.ObjectMetadata;
import com.amazonaws.services.s3.model.S3Object;
import com.amazonaws.util.IOUtils;

import com.wiris.std.Md5;
import com.wiris.std.Std;
import com.wiris.util.sys.Cache;

import java.io.ByteArrayInputStream;
import java.io.InputStream;
import java.io.UnsupportedEncodingException;

import java.util.Properties;

import com.amazonaws.services.s3.model.ListVersionsRequest;
import com.amazonaws.services.s3.model.ObjectListing;
import com.amazonaws.services.s3.model.S3ObjectSummary;
import com.amazonaws.services.s3.model.S3VersionSummary;
import com.amazonaws.services.s3.model.VersionListing;

import java.util.Iterator;

/**
 * Created by redea on 20-Jul-16.
 * Modified from http://www.wiris.com/en/system/files/attachments/1983/JavaS3StorageAndCacheExample.zip
 */
public class S3StorageAndCacheExample implements StorageAndCache {
    // config WIRIS plugin configuration loaded from configuration.ini.
    Properties config;

    // Cache and Formula folders.
    private static final String FORMULA_FOLDER = "formula";
    private static final String CACHE_FOLDER = "cache";
    private static final String PNG_EXT = "png";
    private static final String SVG_EXT = "svg";
    private static final int MAGIC_NUMBER = 2;

    private static final String BUCKET_NAME = "@BUCKET_NAME@";
    private static final String ACCESS_KEY = "@ACCESS_KEY@";
    private static final String SECRET_KEY = "@SECRET_KEY@";
    private static final Region REGION = Region.getRegion(Regions.EU_WEST_1);
    private AWSCredentials credentials = null;
    private AmazonS3Client s3;
    private String bucketName;

    // CacheObjects;
    private Cache cache;
    private Cache cacheFormula;

    /**
     * Given a service like png or svg, returns the associated extension.
     *
     * @return The associated file extension. If the service is not an image servive (svg or png) returns "txt"
     * file extension.
     */
    private static String getExtension(String service) {
        if (service.equals(PNG_EXT)) {
            return "png";
        }
        if (service.equals(SVG_EXT)) {
            return "svg";
        }
        return service + ".txt";
    }


    /**
     * Given a service like png, svg returns the associated Content-Type HTTP header.
     *
     * @return The associated HTTP header. If the service is not an image service(svg or png) returns "text/plain" header
     */
    private static String getExtensionEncoding(String service) {
        if (service.equals(PNG_EXT)) {
            return "image/png";
        }
        if (service.equals(SVG_EXT)) {
            return "image/svg+xml";
        }
        return "text/plain";
    }

    /**
     * Given a md5 digest returns the associated folder structure.
     *
     * @return Associated folder structure.
     */
    private static String getFolderStore(String digest) {
        return Std.substr(digest, 0, MAGIC_NUMBER) + "/" + Std.substr(digest, MAGIC_NUMBER, MAGIC_NUMBER) + "/";
    }

    @Override
    public void init(Object obj, Properties config, Cache cache, Cache cacheFormula) {
        String accessKey;
        String secretKey;
        this.config = config;
        this.cache = cache;
        this.cacheFormula = cacheFormula;

        if (s3 == null) {
            accessKey = ACCESS_KEY;
            secretKey = SECRET_KEY;

            if (accessKey.isEmpty() || secretKey.isEmpty()) {
                this.credentials = new InstanceProfileCredentialsProvider().getCredentials();
            } else {
                this.credentials = new BasicAWSCredentials(accessKey, secretKey);
            }
            s3 = new AmazonS3Client(this.credentials);
            s3.setRegion(REGION);
        }
        bucketName = config.getProperty(BUCKET_NAME);
    }

    @Override
    public String codeDigest(String content) {
        // Using WIRIS Std utils to encode the content to MD5.
        // It is strongly recommended to use MD5 class to generate the md5 string to avoid consistence issues.
        String digest = Md5.encode(content);
        try {
            String key = FORMULA_FOLDER + "/" + getFolderStore(digest) + digest + "." + "ini";
            InputStream in = new ByteArrayInputStream(content.getBytes("UTF-8"));
            ObjectMetadata meta = new ObjectMetadata();
            meta.setContentType("text/plain");
            s3.putObject(BUCKET_NAME, key, in, meta);
        } catch (UnsupportedEncodingException e) {
            //This is to be ignore
        }
        return digest;
    }

    @Override
    public String decodeDigest(String digest) {
        try {
            String key = FORMULA_FOLDER + "/" + getFolderStore(digest) + digest + "." + "ini";
            // Get all of object's metadata and a stream from which to read the contents.
            // See http://docs.aws.amazon.com/AmazonS3/latest/dev/RetrievingObjectUsingJava.html for more information.
            S3Object objectData = s3.getObject(BUCKET_NAME, key);
            // Process the objectData stream.
            String decodedDigest = IOUtils.toString(objectData.getObjectContent());
            // Close the input stream.
            objectData.close();
            return decodedDigest;
        } catch (Exception e) {
            //This is to be ignore
            return null;
        }
    }

    @Override
    public byte[] retreiveData(String digest, String service) {
        try {
            String key = CACHE_FOLDER + "/" + getFolderStore(digest) + digest + "." + getExtension(service);
            S3Object objectData = s3.getObject(BUCKET_NAME, key);
            byte[] b = IOUtils.toByteArray(objectData.getObjectContent());
            objectData.close();
            return b;
        } catch (Exception e) {
            //This is to be ignore
            return null;
        }
    }

    @Override
    public void storeData(String digest, String service, byte[] stream) {
        String key = CACHE_FOLDER + "/" + getFolderStore(digest) + digest + "." + getExtension(service);
        InputStream in = new ByteArrayInputStream(stream);
        ObjectMetadata meta = new ObjectMetadata();
        meta.setContentType(getExtensionEncoding(service));
        s3.putObject(BUCKET_NAME, key, in, meta);
    }

    @Override
    public void deleteCache() {
        try {
            ObjectListing object_listing = s3.listObjects(BUCKET_NAME);
            while (true) {
                for (Iterator<?> iterator = object_listing.getObjectSummaries().iterator(); iterator.hasNext();) {
                    S3ObjectSummary summary = (S3ObjectSummary) iterator.next();
                    s3.deleteObject(BUCKET_NAME, summary.getKey());
                }

                // more object_listing to retrieve?
                if (object_listing.isTruncated()) {
                    object_listing = s3.listNextBatchOfObjects(object_listing);
                } else {
                    break;
                }
            }
            ;

            VersionListing version_listing = s3.listVersions(new ListVersionsRequest().withBucketName(BUCKET_NAME));
            while (true) {
                for (Iterator<?> iterator = version_listing.getVersionSummaries().iterator(); iterator.hasNext();) {
                    S3VersionSummary vs = (S3VersionSummary) iterator.next();
                    s3.deleteVersion(

                        BUCKET_NAME, vs.getKey(), vs.getVersionId());
                }

                if (version_listing.isTruncated()) {
                    version_listing = s3.listNextBatchOfVersions(version_listing);
                } else {
                    break;
                }
            }

        } catch (Exception e) {
            System.out.println("It was not possible delete the cache: " + e.getMessage());
        }
    }
}
