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
   this software without specific permitted.
*/

using Imview.Core.Common;
 
 namespace Imview.Core.Database;
 
 public class WorldDatabase : RavenDatabaseSingleton<WorldDatabase> {
 
     // Always read the latest values from the configuration so changes made at runtime
     // (e.g. from the database configuration dialog) are picked up correctly.
     protected override string DatabaseName
         => ConfigurationManager.Settings["Database.WorldDatabaseName"].AsString("WorldDB");
 
     protected override string Url
         => ConfigurationManager.Settings["Database.WorldDatabaseUrl"].AsString();
 
     protected override string CertificatePath
         => ConfigurationManager.Settings["Database.WorldDatabaseCertificatePath"].AsString();
 
 }
