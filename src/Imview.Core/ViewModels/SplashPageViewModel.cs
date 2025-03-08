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

using ReactiveUI;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace Imview.Core.ViewModels;

public class SplashPageViewModel(MainWindowViewModel mainViewModel) : ViewModelBase {

    public ObservableCollection<SplashSectionViewModel> Sections { get; } = [
        new SplashSectionViewModel(
            "Quests",
            "Create Quest",
            "Load Quest",
            "Get Quests From Packet Capture",
            mainViewModel.CreateNewQuest,
            mainViewModel.LoadQuest,
            mainViewModel.GetQuestsFromPacketCapture),
        new SplashSectionViewModel(
            "Files",
            "Unpack KIWADs",
            "Unpack KIWADs & Deserialize",
            "",
            () => mainViewModel.UnpackKiwad(false),
            () => mainViewModel.UnpackKiwad(true),
            null),
        new SplashSectionViewModel(
            "Object Property",
            "Analyze Blob",
            "",
            "",
            mainViewModel.AnalyzeObjectPropertyBlob,
            null,
            null)
    ];
    
}

public class SplashSectionViewModel(
    string title,
    string firstButtonText,
    string secondButtonText,
    string thirdButtonText,
    System.Action? firstButtonAction,
    System.Action? secontButtonAction,
    System.Action? thirdButtonAction) {

    public string Title { get; } = title;
    public string FirstButtonText { get; } = firstButtonText;
    public string SecondButtonText { get; } = secondButtonText;
    public string ThirdButtonText { get; } = thirdButtonText;
    public ICommand? FirstButtonCommand { get; }
        = firstButtonAction != null
            ? ReactiveCommand.Create(firstButtonAction)
            : null;
    public ICommand? SecondButtonCommand { get; }
        = secontButtonAction != null
            ? ReactiveCommand.Create(secontButtonAction)
            : null;
    public ICommand? ThirdButtonCommand { get; }
        = thirdButtonAction != null
            ? ReactiveCommand.Create(thirdButtonAction)
            : null;
    public bool HasSecondButton
        => !string.IsNullOrEmpty(SecondButtonText)
        && SecondButtonCommand != null;
    public bool HasThirdButton
        => !string.IsNullOrEmpty(ThirdButtonText)
        && ThirdButtonCommand != null;

}