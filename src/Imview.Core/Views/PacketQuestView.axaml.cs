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

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Imcodec.ObjectProperty.TypeCache;
using Imview.Core.ViewModels;

namespace Imview.Core.Views;

public partial class PacketQuestView : UserControl {

   public PacketQuestView() => AvaloniaXamlLoader.Load(this);

   private void OnSaveIndividualQuestClick(object? sender, RoutedEventArgs e) {
        if (sender is Avalonia.Controls.Button button && button.Tag is QuestTemplate quest && DataContext is PacketQuestViewModel viewModel) {
            viewModel.SaveIndividualQuestCommand.Execute(quest);
        }
   }

}