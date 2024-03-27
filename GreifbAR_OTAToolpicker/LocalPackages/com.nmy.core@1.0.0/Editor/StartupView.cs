using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NMY;
using System.Linq;

public class StartupView : EditorWindow
{
	private static StartupBehaviour[] suBehaviours;
	private static Vector2 scrollPosition;

	private static GUIStyle gsTest = new GUIStyle();
	private static GUIStyle positiveStyle = new GUIStyle();
	private static GUIStyle negativeStyle = new GUIStyle();

	private static int selectedIdx = 0;
	private static int currentPage = 0 ;
	private static int totalPages = 1;
	private static int minRange = 0;
	private static int maxRange = 0;

	private static Color[] colors = new Color[] 
	{
		new Color(0.3f, 0.3f, 0.3f),
		new Color(0.2f, 0.2f, 0.2f),
		new Color(0.2f, 0.2f, 0.4f),
		new Color(0.2f, 0.6f, 0.2f),
		new Color(0.6f, 0.2f, 0.2f),
		new Color(0.4f, 0.4f, 0.1f)
	};

	private static Texture2D evenColorTex;
	private static Texture2D oddColorTex;
	private static Texture2D titleColorTex;
	private static Texture2D activeColorTex;
	private static Texture2D nonactiveColorTex;
	private static Texture2D selectedColorTex;

	[MenuItem("NMY/Startup/Startup View")]
	public static void ShowWindow()
	{
		evenColorTex = MakeTex(2, 2, colors[0]);
		oddColorTex = MakeTex(2, 2, colors[1]);
		titleColorTex = MakeTex(2, 2, colors[2]);
		activeColorTex = MakeTex(2, 2, colors[3]);
		nonactiveColorTex = MakeTex(2, 2, colors[4]);
		selectedColorTex = MakeTex(2, 2, colors[5]);

		positiveStyle.normal.background = activeColorTex;
		negativeStyle.normal.background = nonactiveColorTex;

		suBehaviours = GetAllStartupBehaviours();
		GetWindow(typeof(StartupView));
	}

	private static StartupBehaviour[] GetAllStartupBehaviours(){
#if UNITY_2020_1_OR_NEWER // finding inactive objects not available in earlier versions
		StartupBehaviour[] allObjects = FindObjectsOfType<StartupBehaviour>(true);
		totalPages = Mathf.CeilToInt((float)allObjects.Length / 100f);
		return allObjects.OrderBy(x => StaticUtils.GetFullName(x.gameObject)).ToArray();
#else
		List<StartupBehaviour> allObjects=new List<StartupBehaviour>();
        foreach (StartupBehaviour sub in Resources.FindObjectsOfTypeAll(typeof(StartupBehaviour)) as StartupBehaviour[])
        {
            if (!EditorUtility.IsPersistent(sub.transform.root.gameObject) && !(sub.hideFlags == HideFlags.NotEditable || sub.hideFlags == HideFlags.HideAndDontSave))
                allObjects.Add(sub);
        }
		totalPages = Mathf.CeilToInt((float)allObjects.Count / 100f);

		return allObjects.OrderBy(x => StaticUtils.GetFullName(x.gameObject)).ToArray();
#endif
	}

	public static void ShowWindow(int instanceID)
	{
		evenColorTex = MakeTex(2, 2, colors[0]);
		oddColorTex = MakeTex(2, 2, colors[1]);
		titleColorTex = MakeTex(2, 2, colors[2]);
		activeColorTex = MakeTex(2, 2, colors[3]);
		nonactiveColorTex = MakeTex(2, 2, colors[4]);
		selectedColorTex = MakeTex(2, 2, colors[5]);

		positiveStyle.normal.background = activeColorTex;
		negativeStyle.normal.background = nonactiveColorTex;

		suBehaviours = GetAllStartupBehaviours();
		selectedIdx = GetIndexOfTarget(instanceID);

		currentPage = Mathf.FloorToInt(selectedIdx / 100);

		EditorWindow edWin = GetWindow(typeof(StartupView));

		float idxHeight = 1.135f * ((1 + selectedIdx - (currentPage * 100)) * 15) -20;

		//Debug.Log("idxHeight: " + idxHeight + " winH: " + edWin.position.height);
		if (idxHeight < 0)
			idxHeight = 0;

		// no idea how to calculate the correct scrollPosition.y -> using Approximation function: (Data from measurements)
		// y = mx + b
		// wobei:
		//   m = 1.135f
		//   x = selectedIdx - (currentPage * 100)) * 15
		//   b = -20
		// y = 1.135f * (selectedIdx - (currentPage * 100)) * 15) -20


		scrollPosition = new Vector2(0, idxHeight);
		//Debug.Log("select item: " + selectedIdx + " on Page: " + currentPage + " at: " + scrollPosition.y);
	}

	void Init()
	{
		if (suBehaviours == null || suBehaviours.Length == 0 || suBehaviours[0] == null)
		{
			CreateTextures();
			suBehaviours = GetAllStartupBehaviours();
		}
	}

	static void CreateTextures()
	{
		evenColorTex = MakeTex(2, 2, colors[0]);
		oddColorTex = MakeTex(2, 2, colors[1]);
		titleColorTex = MakeTex(2, 2, colors[2]);
		activeColorTex = MakeTex(2, 2, colors[3]);
		nonactiveColorTex = MakeTex(2, 2, colors[4]);
		selectedColorTex = MakeTex(2, 2, colors[5]);

		positiveStyle.normal.background = activeColorTex;
		negativeStyle.normal.background = nonactiveColorTex;
	}

	void OnEnable()
	{
		Init();
	}

	void OnGUI()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("List of all Startups in Scene", EditorStyles.boldLabel, GUILayout.Width(165));
		GUILayout.Label("(" + suBehaviours.Length + " in total)", GUILayout.Width(75)); 
		GUILayout.Space(75);
		if (GUILayout.Button("<<<", GUILayout.Width(35)))
			currentPage--;
		GUILayout.Label((1+currentPage).ToString() + " / " + totalPages.ToString(), EditorStyles.miniButtonMid, GUILayout.Width(45));
		if (GUILayout.Button(">>>", GUILayout.Width(35)))
			currentPage++;
		currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);
		GUILayout.Label(" ");
		if (GUILayout.Button("Refresh", GUILayout.Width(60)))
		{
			suBehaviours = GetAllStartupBehaviours();
		}
		GUILayout.EndHorizontal();
		Init();

		if (suBehaviours == null || suBehaviours.Length == 0)
			return;

		gsTest.normal.background = titleColorTex;
		GUILayout.BeginHorizontal(gsTest);
		GUILayout.Label("Jump to", GUILayout.Width(60));
		GUILayout.Label("|", GUILayout.Width(10));
		GUILayout.Label("GO Name", GUILayout.Width(250));
		GUILayout.Label("|", GUILayout.Width(10));
		GUILayout.Label("Type", GUILayout.Width(250));
		GUILayout.Label("|", GUILayout.Width(10));
		GUILayout.Label("Started up", GUILayout.Width(100));
		GUILayout.Label("|", GUILayout.Width(10));
		GUILayout.Label("Is Activated", GUILayout.Width(100));
		GUILayout.EndHorizontal();

		Event e = Event.current;

		scrollPosition = GUILayout.BeginScrollView(scrollPosition, true, true, GUILayout.Width(position.width), GUILayout.Height(position.height-45));

		if (currentPage == 0)
		{
			minRange = 0;
			maxRange = suBehaviours.Length > 100 ? 100 : suBehaviours.Length;
		}
		else if (currentPage == totalPages-1)
		{
			minRange = currentPage * 100;
			maxRange = minRange + suBehaviours.Length - (currentPage * 100);
		}
		else
		{
			minRange = currentPage * 100;
			maxRange = minRange + 100;
		}

		for (int i = minRange; i < maxRange; i++)
		{
			if (suBehaviours[i] == null)
				continue;

			if (i == selectedIdx)
			{
				gsTest.normal.background = selectedColorTex;
				gsTest.hover.background = selectedColorTex;
			}
			else
			{
				gsTest.normal.background = i % 2 == 0 ? evenColorTex : oddColorTex;
				gsTest.hover.background = titleColorTex;
			}

			GUILayout.BeginHorizontal(gsTest);
			if (GUILayout.Button("Select", GUILayout.Height(14), GUILayout.Width(60)))
			{
				Selection.objects = new UnityEngine.Object[] { suBehaviours[i].gameObject };
			}
			GUILayout.Label("|", GUILayout.Width(10));
			GUILayout.Label(suBehaviours[i].name, GUILayout.Width(250));
			GUILayout.Label("|", GUILayout.Width(10));
			GUILayout.Label(suBehaviours[i].GetType().ToString(), GUILayout.Width(250));
			GUILayout.Label("|", GUILayout.Width(10));
			if (suBehaviours[i].hasStartedUp)
				GUILayout.Label(suBehaviours[i].hasStartedUp.ToString(), positiveStyle, GUILayout.Width(100));
			else
				GUILayout.Label(suBehaviours[i].hasStartedUp.ToString(), negativeStyle, GUILayout.Width(100));

			GUILayout.Label("|", GUILayout.Width(10));
			if (suBehaviours[i].GetType().IsSubclassOf(typeof(ActivatableStartupBehaviour)))
			{
				bool isActivated = ((ActivatableStartupBehaviour)suBehaviours[i]).isActivated;
				if (isActivated)
					GUILayout.Label(isActivated.ToString(), positiveStyle, GUILayout.Width(100));
				else
					GUILayout.Label(isActivated.ToString(), negativeStyle, GUILayout.Width(100));

			}
			GUILayout.EndHorizontal();
			if (e.button == 0 && e.isMouse)
			{
				var rect = GUILayoutUtility.GetLastRect();
				if (rect.Contains(e.mousePosition))
				{
					selectedIdx = i;
				}
			}
			
		}
		GUILayout.EndScrollView();
	}

	private void OnInspectorUpdate()
	{
		// This will only get called 10 times per second.
		Repaint();
	}

	private static int GetIndexOfTarget(int instanceID)
	{
		if (suBehaviours == null || suBehaviours.Length == 0)
			return 0;
		for (int i = 0; i < suBehaviours.Length; i++)
		{
			if (suBehaviours[i].GetInstanceID() == instanceID)
			{
				return i;
			}
		}
		return 0;
	}

	private static Texture2D MakeTex(int width, int height, Color col)
	{
		Color[] pix = new Color[width * height];
		for (int i = 0; i < pix.Length; ++i)
		{
			pix[i] = col;
		}
		Texture2D result = new Texture2D(width, height);
		result.SetPixels(pix);
		result.Apply();
		return result;
	}
}
