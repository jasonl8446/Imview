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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Imcodec.ObjectProperty.TypeCache;
using Imview.Core.Controls.Base;
using Imview.Core.Services;
using System;
using System.Threading.Tasks;

namespace Imview.Core.Controls.Requirements;

/// <summary>
/// Factory for creating requirement editors.
/// </summary>
public interface IRequirementEditorFactory {

    Task<Requirement?> CreateEditor(Requirement? template = null);

}

public class RequirementEditorFactory : IRequirementEditorFactory {

    public async Task<Requirement?> CreateEditor(Requirement? template = null) {
        var editor = new RequirementTemplateEditor(template, RequirementFinderService.RequirementTypes);

        var editorWindow = (EditorWindowBase<Requirement>)editor;
        var appLifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime
            ?? throw new InvalidOperationException("Application lifetime is not set or is not of the expected type.");
        var mainWindow = appLifetime.MainWindow 
            ?? throw new InvalidOperationException("Main window is not set.");
        
        await editor.ShowDialog(mainWindow);
        
        return await editorWindow.GetResultAsync();
    }
    
}