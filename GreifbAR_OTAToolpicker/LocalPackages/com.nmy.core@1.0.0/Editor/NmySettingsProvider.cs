using System.Collections.Generic;
using UnityEditor;

static class NmySettingsProvider
{
	[SettingsProvider]
	public static SettingsProvider CreateNmySettingsProvider()
	{
		// First parameter is the path in the Settings window.
		// Second parameter is the scope of this setting: it only appears in the Project Settings window.
		var provider = new SettingsProvider("Preferences/NMY/Startup", SettingsScope.User)
		{
			// By default the last token of the path is used as display name if no label is provided.
			label = "Startup Tools Settings",
			// Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
			guiHandler = (searchContext) =>
			{
				float originalValue = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = 250; // must hack the global setting, as Unity doesn't provide a solution for EditorGUI
				var preference = EditorPrefs.GetBool("ShowHighlights", defaultValue: false);
				preference = EditorGUILayout.Toggle(label: "Highlight Startups and Activatables", value: preference);
				EditorPrefs.SetBool("ShowHighlights", preference);
				EditorGUIUtility.labelWidth = originalValue; // restore
			},

			// Populate the search keywords to enable smart search filtering and label highlighting:
			keywords = new HashSet<string>(new[] { "NMY", "Startup" })
		};

		return provider;
	}
}
