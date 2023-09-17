// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using Naninovel.UI;
using TMPro;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to manage <see cref="IManagedUI"/> objects.
    /// </summary>
    public interface IUIManager : IEngineService<UIConfiguration>
    {
        /// <summary>
        /// Event invoked when <see cref="FontName"/> is changed.
        /// </summary>
        event Action<string> OnFontNameChanged;
        /// <summary>
        /// Event invoked when <see cref="FontSize"/> index is changed.
        /// </summary>
        event Action<int> OnFontSizeChanged;
        
        /// <summary>
        /// Name of the font option (<see cref="UIConfiguration.FontOptions"/>) to apply for the affected text elements contained in the managed UIs.
        /// Null identifies with default font being used.
        /// </summary>
        string FontName { get; set; }
        /// <summary>
        /// Font size index to apply for the affected text elements contained in the managed UIs.
        /// -1 identifies with default font size being used.
        /// </summary>
        int FontSize { get; set; }

        /// <summary>
        /// Instantiates provided prefab, initializes and adds <see cref="IManagedUI"/> component (should be on the root object of the prefab)
        /// to the managed objects; applies UI-related engine configuration and game settings. Don't forget to <see cref="RemoveUI(IManagedUI)"/> when destroying the game object.
        /// </summary>
        /// <param name="prefab">The prefab to spawn. Should have a <see cref="IManagedUI"/> component attached to the root object.</param>
        /// <param name="name">Unique name of the UI. When not provided will use the prefab name.</param>
        /// <param name="group">Name of game object to group the UI under.</param>
        UniTask<IManagedUI> AddUIAsync (GameObject prefab, string name = default, string group = default);
        /// <summary>
        /// Returns all the UIs managed by the service.
        /// </summary>
        IReadOnlyCollection<IManagedUI> GetManagedUIs ();
        /// <summary>
        /// Tests whether managed UI of specified type is available and can be accessed with <see cref="GetUI{T}"/>.
        /// </summary>
        bool HasUI<T> () where T : class, IManagedUI;
        /// <summary>
        /// Tests whether managed UI of specified resource name is available and can be accessed with <see cref="GetUI"/>.
        /// </summary>
        bool HasUI (string name);
        /// <summary>
        /// Returns managed UI of the provided type <typeparamref name="T"/>.
        /// Results per requested types are cached, so it's fine to use this method frequently.
        /// </summary>
        T GetUI<T> () where T : class, IManagedUI;
        /// <summary>
        /// Returns managed UI of the provided UI resource name.
        /// </summary>
        IManagedUI GetUI (string name);
        /// <summary>
        /// Removes provided UI from the managed objects.
        /// </summary>
        /// <param name="managedUI">Managed UI instance to remove.</param>
        /// <returns>Whether the UI was successfully removed.</returns>
        bool RemoveUI (IManagedUI managedUI);
        /// <summary>
        /// Controls whether the UI (as a whole) is rendered (visible); won't affect visibility state of any particular UI.
        /// Will also spawn <see cref="IClickThroughPanel"/>, which will block input to prevent user from re-showing the UI,
        /// unless <paramref name="allowToggle"/> is true, in which case it'll be possible to re-show the UI with hotkeys and by clicking anywhere on the screen.
        /// </summary>
        void SetUIVisibleWithToggle (bool visible, bool allowToggle = true);
        /// <summary>
        /// Makes provided UI modal, disabling interaction with other non-modal UIs.
        /// </summary>
        void AddModalUI (IManagedUI managedUI);
        /// <summary>
        /// Given provided UI was made modal before, makes it non-modal,
        /// restoring interaction with other UIs in case no other UI is modal.
        /// </summary>
        void RemoveModalUI (IManagedUI managedUI);
        /// <summary>
        /// Returns font asset associated with the specified font name.
        /// </summary>
        TMP_FontAsset GetFontAsset (string fontName);
    }
}
