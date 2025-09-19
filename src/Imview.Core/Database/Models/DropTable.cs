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
using System.Collections.Generic;
using Imcodec.ObjectProperty.TypeCache;

namespace Imview.Core.Database.Models;

public class DropTable {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double RollChance { get; set; } = 1.0;
    public int Weight { get; set; } = 100;
    public double NoneChance { get; set; } = 0.0;
    public double PityCounter { get; set; } = 0.0;
    public List<DropItem> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = Environment.UserName;
    public string ModifiedBy { get; set; } = Environment.UserName;
}

public class DropItem {
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public RequirementList? Requirements { get; set; } = null;
}