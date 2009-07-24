//
// <copyright file="StorageAccountInfo.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Configuration;
using System.Collections.Specialized;
using Microsoft.ServiceHosting.ServiceRuntime;

namespace Microsoft.Samples.ServiceHosting.StorageClient
{
    /// <summary>
    /// Objects of this class encapsulate information about a storage account and endpoint configuration. 
    /// Associated with a storage account is the account name, the base URI of the account and a shared key. 
    /// </summary>
    public class StorageAccountInfo
    {

        /// <summary>
        /// The default configuration string in configuration files for setting the queue storage endpoint.
        /// </summary>
        public static readonly string DefaultQueueStorageEndpointConfigurationString = "QueueStorageEndpoint";

        /// <summary>
        /// The default configuration string in configuration files for setting the blob storage endpoint.
        /// </summary>
        public static readonly string DefaultBlobStorageEndpointConfigurationString = "BlobStorageEndpoint";

        /// <summary>
        /// The default configuration string in configuration files for setting the table storage endpoint.
        /// </summary>
        public static readonly string DefaultTableStorageEndpointConfigurationString = "TableStorageEndpoint";

        /// <summary>
        /// The default configuration string in configuration files for setting the storage account name.
        /// </summary>
        public static readonly string DefaultAccountNameConfigurationString = "AccountName";

        /// <summary>
        /// The default configuration string in configuration files for setting the shared key associated with a storage account.
        /// </summary>
        public static readonly string DefaultAccountSharedKeyConfigurationString = "AccountSharedKey";

        /// <summary>
        /// The default configuration string in configuration files for setting the UsePathStyleUris option.
        /// </summary>
        public static readonly string DefaultUsePathStyleUrisConfigurationString = "UsePathStyleUris";

        /// <summary>
        /// The default prefix string in application config and Web.config files to indicate that this setting should be looked up 
        /// in the fabric's configuration system.
        /// </summary>
        public static readonly string CSConfigStringPrefix = "CSConfigName";

        private bool? _usePathStyleUris;

        /// <summary>
        /// Constructor for creating account info objects.
        /// </summary>
        /// <param name="baseUri">The account's base URI.</param>
        /// <param name="usePathStyleUris">If true, path-style URIs (http://baseuri/accountname/containername/objectname) are used.
        /// If false host-style URIs (http://accountname.baseuri/containername/objectname) are used,
        /// where baseuri is the URI of the service..
        /// If null, the choice is made automatically: path-style URIs if host name part of base URI is an 
        /// IP addres, host-style otherwise.</param>
        /// <param name="accountName">The account name.</param>
        /// <param name="base64Key">The account's shared key.</param>
        public StorageAccountInfo(Uri baseUri, bool? usePathStyleUris, string accountName, string base64Key)
            : this(baseUri, usePathStyleUris, accountName, base64Key, false)
        { 
        }

        /// <summary>
        /// Constructor for creating account info objects.
        /// </summary>
        /// <param name="baseUri">The account's base URI.</param>
        /// <param name="usePathStyleUris">If true, path-style URIs (http://baseuri/accountname/containername/objectname) are used.
        /// If false host-style URIs (http://accountname.baseuri/containername/objectname) are used,
        /// where baseuri is the URI of the service.
        /// If null, the choice is made automatically: path-style URIs if host name part of base URI is an 
        /// IP addres, host-style otherwise.</param>
        /// <param name="accountName">The account name.</param>
        /// <param name="base64Key">The account's shared key.</param>
        /// <param name="allowIncompleteSettings">true if it shall be allowed to only set parts of the StorageAccountInfo properties.</param>
        public StorageAccountInfo(Uri baseUri, bool? usePathStyleUris, string accountName, string base64Key, bool allowIncompleteSettings)
        {
            if (baseUri == null && !allowIncompleteSettings)
            {
                throw new ArgumentNullException("baseUri");
            }
            if (string.IsNullOrEmpty(accountName) && !allowIncompleteSettings)
            {
                throw new ArgumentNullException("accountName");
            }
            if (string.IsNullOrEmpty(base64Key) && !allowIncompleteSettings)
            {
                throw new ArgumentNullException("base64Key");
            }

            BaseUri = baseUri;
            AccountName = accountName;
            Base64Key = base64Key;
            if (usePathStyleUris == null && baseUri == null && !allowIncompleteSettings) {
                throw new ArgumentException("Cannot determine setting from empty URI.");
            }
            else if (usePathStyleUris == null)
            {
                _usePathStyleUris = null;
            }
            else
            {
                UsePathStyleUris = usePathStyleUris.Value;
            }
        }

        /// <summary>
        /// The base URI of the account.
        /// </summary>
        public Uri BaseUri
        {
            get;
            set;
        }

        /// <summary>
        /// The account name.
        /// </summary>
        public string AccountName
        {
            get;
            set;
        }

        /// <summary>
        /// The account's key.
        /// </summary>
        public string Base64Key
        {
            get;
            set;
        }

        /// <summary>
        /// If set, returns the UsePathStyleUris properties. If the property has not been explicitly set, 
        /// the implementation tries to derive the correct value from the base URI.
        /// </summary>
        public bool UsePathStyleUris
        {
            get
            {
                if (_usePathStyleUris == null)
                {
                    if (BaseUri == null)
                    {
                        return false;
                    }
                    else
                    {
                        return Utilities.StringIsIPAddress(BaseUri.Host);
                    }
                }
                else
                {
                    return _usePathStyleUris.Value;
                }
            }
            set {
                _usePathStyleUris = value;
            }
        }

        /// <summary>
        /// Retrieves account settings for the queue service from the default settings. 
        /// </summary>
        public static StorageAccountInfo GetDefaultQueueStorageAccountFromConfiguration(bool allowIncompleteSettings)
        {
            return GetAccountInfoFromConfiguration(DefaultQueueStorageEndpointConfigurationString, allowIncompleteSettings);
        }

        /// <summary>
        /// Retrieves account settings for the queue service from the default settings. 
        /// Throws an exception in case of incomplete settings.
        /// </summary>
        public static StorageAccountInfo GetDefaultQueueStorageAccountFromConfiguration()
        {
            return GetAccountInfoFromConfiguration(DefaultQueueStorageEndpointConfigurationString, false);
        }

        /// <summary>
        /// Retrieves account settings for the table service from the default settings. 
        /// </summary>
        public static StorageAccountInfo GetDefaultTableStorageAccountFromConfiguration(bool allowIncompleteSettings)
        {
            return GetAccountInfoFromConfiguration(DefaultTableStorageEndpointConfigurationString, allowIncompleteSettings);
        }

        /// <summary>
        /// Retrieves account settings for the table service from the default settings. 
        /// Throws an exception in case of incomplete settings.
        /// </summary>
        public static StorageAccountInfo GetDefaultTableStorageAccountFromConfiguration()
        {
            return GetAccountInfoFromConfiguration(DefaultTableStorageEndpointConfigurationString, false);
        }

        /// <summary>
        /// Retrieves account settings for the blob service from the default settings. 
        /// </summary>
        public static StorageAccountInfo GetDefaultBlobStorageAccountFromConfiguration(bool allowIncompleteSettings)
        {
            return GetAccountInfoFromConfiguration(DefaultBlobStorageEndpointConfigurationString, allowIncompleteSettings);
        }

        /// <summary>
        /// Retrieves account settings for the blob service from the default settings. 
        /// Throws an exception in case of incomplete settings.
        /// </summary>
        public static StorageAccountInfo GetDefaultBlobStorageAccountFromConfiguration()
        {
            return GetAccountInfoFromConfiguration(DefaultBlobStorageEndpointConfigurationString, false);
        }

        /// <summary>
        /// Gets settings from default configuration names except for the endpoint configuration string.
        /// </summary>
        public static StorageAccountInfo GetAccountInfoFromConfiguration(string endpointConfiguration, bool allowIncompleteSettings) 
        {
            return GetAccountInfoFromConfiguration(DefaultAccountNameConfigurationString, 
                                       DefaultAccountSharedKeyConfigurationString,
                                       endpointConfiguration, 
                                       DefaultUsePathStyleUrisConfigurationString,
                                       allowIncompleteSettings);
        }

        /// <summary>
        /// Gets settings from default configuration names except for the endpoint configuration string. Throws an exception 
        /// in the case of incomplete settings.
        /// </summary>
        public static StorageAccountInfo GetAccountInfoFromConfiguration(string endpointConfiguration)
        {
            return GetAccountInfoFromConfiguration(DefaultAccountNameConfigurationString,
                                       DefaultAccountSharedKeyConfigurationString,
                                       endpointConfiguration,
                                       DefaultUsePathStyleUrisConfigurationString,
                                       false);
        }

        /// <summary>
        /// Gets a configuration setting from application settings in the Web.config or App.config file. 
        /// When running in a hosted environment, configuration settings are read from .cscfg
        /// files.
        /// </summary>
        public static string GetConfigurationSetting(string configurationSetting, string defaultValue, bool throwIfNull)
        {
            if (string.IsNullOrEmpty(configurationSetting))
            {
                throw new ArgumentException("configurationSetting cannot be empty or null", "configurationSetting");
            }

            string ret = null;

            // first, try to read from appsettings
            ret = TryGetAppSetting(configurationSetting);

            // settings in the csc file overload settings in Web.config
            if (RoleManager.IsRoleManagerRunning)
            {
                string cscRet = TryGetConfigurationSetting(configurationSetting);
                if (!string.IsNullOrEmpty(cscRet))
                {
                    ret = cscRet;
                }

                // if there is a csc config name in the app settings, this config name even overloads the 
                // setting we have right now
                string refWebRet = TryGetAppSetting(StorageAccountInfo.CSConfigStringPrefix + configurationSetting);
                if (!string.IsNullOrEmpty(refWebRet))
                {
                    cscRet = TryGetConfigurationSetting(refWebRet);
                    if (!string.IsNullOrEmpty(cscRet))
                    {
                        ret = cscRet;
                    }
                }
            }

            // if we could not retrieve any configuration string set return value to the default value
            if (string.IsNullOrEmpty(ret) && defaultValue != null)
            {
                ret = defaultValue;
            }

            if (string.IsNullOrEmpty(ret) && throwIfNull)
            {
                throw new ConfigurationErrorsException(
                    string.Format(CultureInfo.InvariantCulture, "Cannot find configuration string {0}.", configurationSetting));
            }
            return ret;
        }

        /// <summary>
        /// Retrieves account information settings from configuration settings. First, the implementation checks for 
        /// settings in an application config section of an app.config or Web.config file. These values are overwritten 
        /// if the same settings appear in a .csdef file.
        /// The implementation also supports indirect settings. In this case, indirect settings overwrite all other settings.
        /// </summary>        
        /// <param name="accountNameConfiguration">Configuration string for the account name.</param>
        /// <param name="accountSharedKeyConfiguration">Configuration string for the key.</param>
        /// <param name="endpointConfiguration">Configuration string for the endpoint.</param>
        /// <param name="usePathStyleUrisConfiguration">Configuration string for the path style.</param>
        /// <param name="allowIncompleteSettings">If false, an exception is thrown if not all settings are available.</param>
        /// <returns>StorageAccountInfo object containing the retrieved settings.</returns>        
        public static StorageAccountInfo GetAccountInfoFromConfiguration(string accountNameConfiguration,
                                                                         string accountSharedKeyConfiguration,
                                                                         string endpointConfiguration, 
                                                                         string usePathStyleUrisConfiguration,
                                                                         bool allowIncompleteSettings)
        {
            if (string.IsNullOrEmpty(endpointConfiguration))
            {
                throw new ArgumentException("Endpoint configuration is missing", "endpointConfiguration");
            }
            string endpoint = null;
            string name = null;
            string key = null;
            string pathStyle = null;

            name = TryGetAppSetting(accountNameConfiguration);
            key = TryGetAppSetting(accountSharedKeyConfiguration);
            endpoint = TryGetAppSetting(endpointConfiguration);
            pathStyle = TryGetAppSetting(usePathStyleUrisConfiguration);


            // settings in the csc file overload settings in Web.config
            if (RoleManager.IsRoleManagerRunning)
            {
                // get config settings from the csc file
                string cscName = TryGetConfigurationSetting(accountNameConfiguration);
                if (!string.IsNullOrEmpty(cscName))
                {
                    name = cscName;
                }
                string cscKey = TryGetConfigurationSetting(accountSharedKeyConfiguration);
                if (!string.IsNullOrEmpty(cscKey))
                {
                    key = cscKey;
                }
                string cscEndpoint = TryGetConfigurationSetting(endpointConfiguration);
                if (!string.IsNullOrEmpty(cscEndpoint))
                {
                    endpoint = cscEndpoint;
                }
                string cscPathStyle = TryGetConfigurationSetting(usePathStyleUrisConfiguration);
                if (!string.IsNullOrEmpty(cscPathStyle))
                {
                    pathStyle = cscPathStyle;
                }

                // the Web.config can have references to csc setting strings
                // these count event stronger than the direct settings in the csc file
                string refWebName = TryGetAppSetting(CSConfigStringPrefix + accountNameConfiguration);
                if (!string.IsNullOrEmpty(refWebName))
                {
                    cscName = TryGetConfigurationSetting(refWebName);
                    if (!string.IsNullOrEmpty(cscName))
                    {
                        name = cscName;
                    }
                }
                string refWebKey = TryGetAppSetting(CSConfigStringPrefix + accountSharedKeyConfiguration);
                if (!string.IsNullOrEmpty(refWebKey))
                {
                    cscKey = TryGetConfigurationSetting(refWebKey);
                    if (!string.IsNullOrEmpty(cscKey))
                    {
                        key = cscKey;
                    }
                }
                string refWebEndpoint = TryGetAppSetting(CSConfigStringPrefix + endpointConfiguration);
                if (!string.IsNullOrEmpty(refWebEndpoint))
                {
                    cscEndpoint = TryGetConfigurationSetting(refWebEndpoint);
                    if (!string.IsNullOrEmpty(cscEndpoint))
                    {
                        endpoint = cscEndpoint;
                    }
                }
                string refWebPathStyle = TryGetAppSetting(CSConfigStringPrefix + usePathStyleUrisConfiguration);
                if (!string.IsNullOrEmpty(refWebPathStyle))
                {
                    cscPathStyle = TryGetConfigurationSetting(refWebPathStyle);
                    if (!string.IsNullOrEmpty(cscPathStyle))
                    {
                        pathStyle = cscPathStyle;
                    }
                }
            }

            if (string.IsNullOrEmpty(name) && !allowIncompleteSettings)
            {
                throw new ArgumentException("No account name specified!");
            }
            if (string.IsNullOrEmpty(key) && !allowIncompleteSettings)
            {
                throw new ArgumentException("No account key specified!");
            }
            if (string.IsNullOrEmpty(endpoint) && !allowIncompleteSettings)
            {
                throw new ArgumentException("No endpoint specified!");
            }

            bool? usePathStyleUris = null;
            if (!string.IsNullOrEmpty(pathStyle))
            {
                bool b;
                if (!bool.TryParse(pathStyle, out b))
                {
                    throw new ConfigurationErrorsException("Cannot parse value of setting UsePathStyleUris as a boolean");
                }
                usePathStyleUris = b;
            }
            Uri tmpBaseUri = null;
            if (!string.IsNullOrEmpty(endpoint))
            {
                tmpBaseUri = new Uri(endpoint);
            }
            return new StorageAccountInfo(tmpBaseUri, usePathStyleUris, name, key, allowIncompleteSettings);
        }


        /// <summary>
        /// Checks whether all essential properties of this object are set. Only then, the account info object 
        /// should be used in ohter APIs of this library.
        /// </summary>
        /// <returns></returns>
        public bool IsCompleteSetting()
        {
            return BaseUri != null && Base64Key != null && AccountName != null;
        }

        /// <summary>
        /// Checks whether this StorageAccountInfo object is complete in the sense that all properties are set.
        /// </summary>
        public void CheckComplete()
        {
            if (!IsCompleteSetting())
            {
                throw new ConfigurationErrorsException("Account information incomplete!");
            }
        }

        #region Private methods

        private static string TryGetConfigurationSetting(string configName)
        {
            string ret = null;
            try
            {
                ret = RoleManager.GetConfigurationSetting(configName);
            }
            catch (RoleException)
            {
                return null;
            }
            return ret;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", 
                                                          Justification = "Make sure that nothing prevents us to read from the fabric's configuration envrionment.")]
        private static string TryGetAppSetting(string configName)
        {
            string ret = null;
            try
            {
                ret = ConfigurationSettings.AppSettings[configName];
            }
            // some exception happened when accessing the app settings section
            // most likely this is because there is no app setting file
            // we assume that this is because there is no app settings file; this is not an error
            // and explicitly all exceptions are captured here
            catch (Exception)
            {
                return null;
            }
            return ret;
        }

        #endregion

    }
}
