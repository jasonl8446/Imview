/*
BSD 3-Clause License

Copyright (c) 2024, Jooty

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.
*/

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Imview.Core.Common;
using Raven.Client.Http;
using System.Net.Security;

namespace Imview.Core.Database;

public abstract class RavenDatabaseSingleton<T> where T : RavenDatabaseSingleton<T> {

    // Singleton manager: only one instance of the database is allowed to exist.
    // Make it lazy so that it is only created when needed.
    private static readonly Lazy<T> s_lazy = new(() => (Activator.CreateInstance(typeof(T), true) as T)!);
    public static T Instance => s_lazy.Value;

    protected readonly byte MaxNumberOfRequestsPerSession
        = ConfigurationManager.Settings["Database.DatabaseMaxNumberOfRequestsPerSession"].AsByte();
    protected readonly byte RequestTimeoutInSeconds
        = ConfigurationManager.Settings["Database.DatabaseRequestTimeoutInSeconds"].AsByte();
    protected readonly byte WaitForNonStaleResultsTimeoutInSeconds
        = ConfigurationManager.Settings["Database.DatabaseWaitForNonStaleResultsTimeout"].AsByte();

    protected abstract string DatabaseName { get; }
    protected abstract string Url { get; }
    protected abstract string CertificatePath { get; }

    protected virtual X509Certificate2? Certificate =>
        string.IsNullOrEmpty(Url) ? null : GetCertificate();

    // Create the store if it doesn't exist, otherwise return the existing store.
    protected IDocumentStore? _store;
    public IDocumentStore? Store => _store ??= CreateStore();

    protected virtual IDocumentStore? CreateStore() {
        if (string.IsNullOrEmpty(Url)) {
            Console.WriteLine("No database URL configured.");
            return null;
        }

        // If this is the first time we're creating the store, we need to create the database.
        Console.WriteLine("Initializing remote RavenDB database connection..");
        Console.WriteLine($"Database URL: {Url}");
        Console.WriteLine($"Database name: {DatabaseName}");

        var store = new DocumentStore {
            Urls = [Url],
            Database = DatabaseName,
            Conventions = {
                MaxNumberOfRequestsPerSession = MaxNumberOfRequestsPerSession,
                UseOptimisticConcurrency = true,
                RequestTimeout = TimeSpan.FromSeconds(RequestTimeoutInSeconds),
                WaitForNonStaleResultsTimeout = TimeSpan.FromSeconds(WaitForNonStaleResultsTimeoutInSeconds),
            },
            Certificate = Certificate
        };

        // Add the certificate validation callback for self-signed certificates.
        // TODO: In a production environment, ideally you should never accept self-signed certificates.
        // This is only for development purposes.
        RequestExecutor.RemoteCertificateValidationCallback += (message, cert, chain, sslPolicyErrors) => {
            if (sslPolicyErrors == SslPolicyErrors.None) {
                return true;
            }

            Console.WriteLine($"Accepting self-signed certificate: {cert.Subject}");
            
            return true;
        };

        store.Initialize();
        Console.WriteLine("Database initialized.");

        return store;
    }

    protected virtual X509Certificate2? GetCertificate() {
        if (string.IsNullOrEmpty(Url) || string.IsNullOrEmpty(CertificatePath)) {
            return null;
        }

        // The certificate path is relative to the working directory.
        // We need to get the absolute path.
        var absolutePath = Path.GetFullPath(CertificatePath);

        // If there is no file at this path, log an error and return null.
        if (!File.Exists(absolutePath)) {
            Console.WriteLine($"No certificate found at path {absolutePath}");
            return null;
        }

        return new X509Certificate2(absolutePath);
    }

    public CollectionStatistics? GetDatabaseStatistics()
        => Store?.Maintenance.Send(new GetCollectionStatisticsOperation());

}