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
using System.Threading.Tasks;
using System.Windows.Input;
using Imcodec.ObjectProperty;
using Imcodec.CoreObject;
using Imview.Core.Services;
using ReactiveUI;
using Newtonsoft.Json;

namespace Imview.Core.ViewModels;

public class ObjectPropertyBlobViewModel : ViewModelBase {

    public ICommand DeserializeBlobCommand { get; }
    public ICommand BackToSplashCommand { get; }

    private readonly MainWindowViewModel _mainViewModel;
    private readonly List<uint> _commonPropertyFlags = [
        1, 6, 7, 16, 19, 23, 24, 25, 27, 30, 31, 39,
        55, 59, 63, 71, 134, 135, 159, 263, 287, 519, 551,
        575, 65536, 65543, 65567, 65664, 65671, 65695, 65703,
        65727, 66087, 131079, 131103, 131207, 131335, 131359,
        262151, 262279, 1048583, 2097159, 2097183, 2097215, 4194439,
        8388615, 8388639, 8388871, 16777223, 16777247, 16777279,
        33554439, 33554463, 33554471, 33554491, 33554495, 33554695,
        33554719, 134217735, 134217759, 136314887, 268435463, 268435487,
    ];
    private string _hexBlob = string.Empty;
    private string _deserializedResult = string.Empty;
    private bool _isDeserializing;

    private static readonly JsonSerializerSettings s_serializerOptions = new() {
        Formatting = Formatting.Indented,
        Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
    };

    public string HexBlob {
        get => _hexBlob;
        set => this.RaiseAndSetIfChanged(ref _hexBlob, value);
    }

    public string DeserializedResult {
        get => _deserializedResult;
        set => this.RaiseAndSetIfChanged(ref _deserializedResult, value);
    }

    public bool IsDeserializing {
        get => _isDeserializing;
        set => this.RaiseAndSetIfChanged(ref _isDeserializing, value);
    }

    public ObjectPropertyBlobViewModel(MainWindowViewModel mainViewModel) {
        _mainViewModel = mainViewModel;
        DeserializeBlobCommand = ReactiveCommand.CreateFromTask(DeserializeBlob);
        BackToSplashCommand = ReactiveCommand.Create(BackToSplash);
    }

    private async Task DeserializeBlob() {
        IsDeserializing = true;
        DeserializedResult = string.Empty;
        var cleanedHexBlob = HexBlob
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "");

        var bytes = Convert.FromHexString(cleanedHexBlob);

        // Create configurations for all serializers to try.
        var serializerConfigs = new List<(string Name, Func<ObjectSerializer> Factory, bool IsVerbose)> {
                // Standard serializers.
                ("ObjectCompact",               static () => new ObjectSerializer(false, SerializerFlags.None), false),
                ("ObjectCompactCompressed",     static () => new ObjectSerializer(false, SerializerFlags.UseFlags | SerializerFlags.Compress), false),
                ("ObjectVerbose",               static () => new ObjectSerializer(true, SerializerFlags.None), true),
                ("ObjectVerboseCompressed",     static () => new ObjectSerializer(true, SerializerFlags.UseFlags | SerializerFlags.Compress), true),
                // Core serializers.
                ("CoreObject",                  static () => new CoreObjectSerializer(false, SerializerFlags.None), false),
                ("CoreObjectCompressed",        static () => new CoreObjectSerializer(false, SerializerFlags.UseFlags | SerializerFlags.Compress), false),
                ("CoreObjectVerbose",           static () => new CoreObjectSerializer(true, SerializerFlags.None), true),
                ("CoreObjectVerboseCompressed", static () => new CoreObjectSerializer(true, SerializerFlags.UseFlags | SerializerFlags.Compress), true)
            };

        foreach (var (Name, Factory, IsVerbose) in serializerConfigs) {
            var serializer = Factory();

            foreach (var flag in _commonPropertyFlags) {
                try {
                    if (serializer.Deserialize<PropertyClass>(bytes, flag, out var propertyClass)) {
                        var info = new {
                            _rawBlob = cleanedHexBlob,
                            _deserializedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            _imcodecVersion = typeof(ObjectPropertyBlobViewModel).Assembly.GetName()?.Version?.ToString() ?? "Unknown",
                            _flags = flag,
                            _serializerType = Name,
                            _verbose = IsVerbose,
                            _objectType = propertyClass!.GetType().Name,
                            _object = propertyClass
                        };

                        DeserializedResult = JsonConvert.SerializeObject(info, s_serializerOptions);
                        IsDeserializing = false;

                        return;
                    }
                }
                catch {
                    continue;
                }
            }
        }

        IsDeserializing = false;
    }

    private void BackToSplash()
        => _mainViewModel.ReturnToSplash();

}