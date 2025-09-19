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

using Imcodec.ObjectProperty.TypeCache;
using Imview.Core.Controls.Base;
using System;
using System.Threading.Tasks;

namespace Imview.Core.Controls.Results;

public static class ResultEditorFactory {
    
    public static Avalonia.Controls.Window? CreateEditor(Result result) {
        return result switch {
            ResAddGold gold => new ResAddGoldEditor(gold),
            ResAddMagicXP magicXP => new ResAddMagicXPEditor(magicXP),
            ResDropTable dropTable => new ResDropTableEditor(dropTable),
            ResAddDynaMod addDynaMod => new ResAddDynaModEditor(addDynaMod),
            ResRemoveDynaMod removeDynaMod => new ResRemoveDynaModEditor(removeDynaMod),
            ResActorDialog actorDialog => new ResActorDialogEditor(actorDialog),
            ResTeleport teleport => new ResTeleportEditor(teleport),
            ResPlaySound playSound => new ResPlaySoundEditor(playSound),
            ResPostEvent postEvent => new ResPostEventEditor(postEvent),
            ResModifyEntry modifyEntry => new ResModifyEntryEditor(modifyEntry),
            ResWait wait => new ResWaitEditor(wait),
            _ => null
        };
    }
    
    public static async Task<Result?> GetResultFromEditor(Avalonia.Controls.Window editor) {
        return editor switch {
            ResAddGoldEditor goldEditor => await goldEditor.GetResultAsync(),
            ResAddMagicXPEditor xpEditor => await xpEditor.GetResultAsync(),
            ResDropTableEditor dropEditor => await dropEditor.GetResultAsync(),
            ResAddDynaModEditor addEditor => await addEditor.GetResultAsync(),
            ResRemoveDynaModEditor removeEditor => await removeEditor.GetResultAsync(),
            ResActorDialogEditor dialogEditor => await dialogEditor.GetResultAsync(),
            ResTeleportEditor teleportEditor => await teleportEditor.GetResultAsync(),
            ResPlaySoundEditor playSoundEditor => await playSoundEditor.GetResultAsync(),
            ResPostEventEditor postEventEditor => await postEventEditor.GetResultAsync(),
            ResModifyEntryEditor modifyEntryEditor => await modifyEntryEditor.GetResultAsync(),
            ResWaitEditor waitEditor => await waitEditor.GetResultAsync(),
            _ => null
        };
    }
    
    public static string GetResultDisplayName(Result result) {
        return result switch {
            ResAddGold gold => $"Add Gold: {gold.m_gold}",
            ResAddMagicXP magicXP => $"Add Magic XP: {magicXP.m_experience} (School: {magicXP.m_magicSchool})",
            ResDropTable dropTable => $"Drop Table: {dropTable.m_tableName} (Max Rolls: {dropTable.m_maxRolls})",
            ResAddDynaMod addDynaMod => $"Add DynaMod: {addDynaMod.m_dynaModClientTag}",
            ResRemoveDynaMod removeDynaMod => $"Remove DynaMod: {removeDynaMod.m_dynaModClientTag}",
            ResActorDialog actorDialog => $"Actor Dialog: {actorDialog.m_activePersona} ({actorDialog.m_registryEntry})",
            ResTeleport teleport => $"Teleport: {teleport.m_destinationZone} -> {teleport.m_destinationLoc}",
            ResPlaySound playSound => $"Play Sound: {playSound.m_soundName} (Blocking: {playSound.m_blocking})",
            ResPostEvent postEvent => $"Post Event: {postEvent.m_eventName}",
            ResModifyEntry modifyEntry => $"Modify Entry: {modifyEntry.m_entryName} = {modifyEntry.m_value}",
            ResWait wait => $"Wait: {wait.m_secondsToWait} seconds",
            _ => result.GetType().Name
        };
    }
}