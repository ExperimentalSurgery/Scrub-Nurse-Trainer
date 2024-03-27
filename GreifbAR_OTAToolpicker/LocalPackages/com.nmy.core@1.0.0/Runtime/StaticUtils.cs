using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// The NMY namespace.
/// </summary>
namespace NMY {

	public class StaticUtils {

	/*
		System.Version ver=System.Environment.OSVersion.Version;

		Name						major.minor	PlatformID
		--------------------------------------------------
		Windows 10					10.0*		2
		Windows Server 2016			10.0*		2
		Windows 8.1					6.3*		2
		Windows Server 2012 R2		6.3*		2
		Windows 8					6.2			2
		Windows Server 2012			6.2			2
		Windows 7					6.1			2
		Windows Server 2008 R2		6.1			2
		Windows Server 2008			6.0			2
		Windows Vista				6.0			2
		Windows Server 2003 R2		5.2			2
		Windows Home Server			5.2			2
		Windows Server 2003			5.2			2
		Windows XP 64-Bit Edition	5.2			2
		Windows XP 					5.1			2
		Windows 2000				5.0			2
		Windows NT 4.0				4.0			2
		Windows NT 3.51				3.?			2
		Windows ME					4.90		1
		Windows 98					4.10		1
		Windows 95					4.0			1

		System.PlatformID plid=System.Environment.OSVersion.Platform;
		-------------------------------------------------------------
		Win32S			0
		Win32Windows	1
		Win32NT			2
		WinCE			3
		Unix			4
		Xbox			5
		MacOSX			6

		*) "For applications that have been manifested for Windows 8.1 or Windows 10." Otherwise it will say 6.2.
		sources: official https://msdn.microsoft.com/en-us/library/windows/desktop/ms724833(v=vs.85).aspx
				 mainly http://stackoverflow.com/questions/2819934/detect-windows-7-in-net
		...there are about 2.4 Gazillion more options. Read http://www.h2net.cz/source/OsVersionInfo.cs and weep...
	*/
			
		// NOTE: we only care about "is this a windows standalone .exe",
		// not "is the OS actaully windows" (which might be true for Webplayer+WebGL, too)
		static public bool isWin {
			get {
#if UNITY_STANDALONE_WIN
				return true;
#else
				return false;
#endif
			}
		}
		
		static public bool isWin7 {
			get {
#if UNITY_STANDALONE_WIN
				System.Version ver=System.Environment.OSVersion.Version;
				return (ver.Major==6 && ver.Minor==1); // note: returns false for Win8! (which is what we want)
#else
				return false;
#endif
			}
		}

		// "isWin8 or above", assuming the touch functionality hasn't changed (yet again) in Win10...
		static public bool isWin8 {
			get {
#if UNITY_STANDALONE_WIN
				System.Version ver=System.Environment.OSVersion.Version;
				return (ver.Major>6 || ver.Major==6 && ver.Minor>=2);
#else
				return false;
#endif
			}
		}

		public static void ForceInvariantCulture(){
			var ci = System.Globalization.CultureInfo.InvariantCulture;
#if NET_4_6 // defined since 2018.3, if .NET 4.x is enabled. WARNING! the following commands exist only for .NET>=4.5! if using an older version, note that CultureInfo will be wrong for all new threads.
			System.Globalization.CultureInfo.DefaultThreadCurrentCulture = ci; 
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = ci;
#endif
			System.Threading.Thread.CurrentThread.CurrentCulture = ci;
            System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
		}

		/// <summary>
		/// Finds all objects of given type.
		/// <para>This is just a convenience template for the Unity function
		/// of the same name</para>
		/// </summary>
		/// <param name='T'>
		/// The type (=class name) to find.
		/// </param>
		/// <returns>The GameObject with the given name, or null.</returns>
		[System.Obsolete("Please use the builtin method FindObjectsOfType<> instead.")]
		public static T[] FindObjectsOfType<T>() {
			T[] objects = UnityEngine.Object.FindObjectsOfType(typeof(T)) as T[];
			return objects;
		}	
		
		/// <summary>
		/// Finds all objects by given name/substring
		/// </summary>
		/// <param name='root'>
		/// The root transform of the hierarchy to search.
		/// </param>
		/// <param name='name'>
		/// The type (=class name) to find.
		/// </param>
		/// <param name='substring'>
		/// If true, match by substring.
		/// If false, the whole name must match exactly.
		/// Defaults to 'true'.
		/// </param>
		/// <param name='collector'>
		/// The internally passed list of matching GameObjects
		/// </param>
		/// <remarks>
		/// As opposed to StaticUtils.Find(...), which does the same returning the FIRST object found instead of ALL,
		/// this version defaults to checking for substring matches (=substring==true vs. fullName==true).
		/// </remarks>
		/// <returns>A list of GameObject with matching name, or null.</returns>
		public static List<GameObject> FindObjectsByName(Transform root, string name, bool substring=true, List<GameObject> collector=null) {
			if(collector==null)
				collector = new List<GameObject>();

			if(root.name==name || substring && root.name.Contains(name))
				collector.Add(root.gameObject);
			foreach(Transform child in root)
				FindObjectsByName(child,name,substring,collector);

			if(collector.Count==0)
				return null; // explicitly return null if empty
			return collector;
		}	
		
		/// <summary>
		/// Find a game object by name.
		/// <para>If no game object with the given name can be found, null is 
		/// returned and an error is logged to the Unity console. If name 
		/// contains a '/' character it will traverse the hierarchy like a path 
		/// name. This function only returns active game objects.</para>
		/// <para>This function simply calls Unity's built-in function 
		/// GameObject.Find(), but includes error logging.</para>
		/// </summary>
		/// <param name='name'>
		/// The name of the GameObject to find.
		/// </param>
		/// <returns>The GameObject with the given name, or null.</returns>
		static public GameObject Find(string name) {
			GameObject obj = GameObject.Find(name);
			if (obj == null)
				Debug.LogWarning("Did not find any object named \"" + name + "\".");
			return obj;
		}
		
		/// <summary>
		/// Find a game object named name in a subtree defined by the given root 
		/// game object.
		/// <para>Returns the game object found, or null if no game object named 
		/// name exists in the subtree specified by root game object.</para>
		/// </summary>
		/// <param name='root'>The root node for the search operation.</param>
		/// <param name='name'>The name of the game object to find.</param> 
		/// <param name='fullName'>false: only look for substring</param>
		static public GameObject Find(GameObject root, string name, bool fullName=true) {
			Transform t=Find(root.transform,name,fullName);
			if(t)
				return t.gameObject;
			Debug.LogWarning("Did not find any object named \"" + name + "\" under root object \""+ root.name + "\".");
			return null;
		}
		
		/// <summary>
		/// Find a transform by name in subtree. The first object found will be 
		/// return. Cannot print an error on null, since we are recursive.
		/// </summary>
		/// <param name='root'>The transform to start the search at.</param>
		/// <param name='name'>The name of the game object to find.</param>
		/// <param name='fullName'>false: only look for substring</param>
		static public Transform Find(Transform root, string name, bool fullName=true) {
			if(fullName && root.name==name ||
					!fullName && root.name.Contains(name))
				return root;

			Transform t;
			foreach(Transform child in root){
				t=Find(child,name,fullName);
				if(t)
					return t;
			}
//			Debug.LogWarning("Did not find any transform named \"" + name + "\" under root transform \""+ root.name + "\".");
			return null;
		}

		static public T GetComponentInParents<T>(GameObject root) where T : Component {
				if (root.transform.parent == null) return null;
				GameObject p = root.transform.parent.gameObject;
				Component c = p.GetComponent<T>();
				
				if (c == null) {
					c = GetComponentInParents<T>(p);
				}
			
				return (T)c;
		}
			
		static public T[] GetComponentsInSelfAndParents<T>(GameObject root) where T : Component {
			System.Collections.Generic.List<T> comps = new System.Collections.Generic.List<T>();
			Transform parent=root.transform;
				
			do{
				foreach (T c in parent.GetComponents<T>())
					comps.Add(c);
				parent=parent.parent;
			}while(parent!=null);
				
			return comps.ToArray();
		}
		
		static public T[] GetComponentsInChildrenNonRecursive<T>(GameObject root) where T : Component {
			return GetComponentsInChildrenNonRecursive<T>(root,false);
		}
			
		static public T[] GetComponentsInChildrenNonRecursive<T>(GameObject root, bool includeInactive) where T : Component {
			System.Collections.Generic.List<T> comps = new System.Collections.Generic.List<T>();
			T c;
			foreach (Transform t in root.transform) {
				if ((c = t.GetComponent<T>()) != null && (includeInactive||t.gameObject.activeInHierarchy))
					comps.Add(c);
			}
			return comps.ToArray();
		}
		
		static public string GetFullName(GameObject root){
			if(!root)
				return "null!";
			string fullName=root.name;
			Transform walker=root.transform.parent;
			while(walker){
				fullName=walker.name+"/"+fullName;
				walker=walker.parent;
			}
			return fullName;
		}

		/// <summary>
		/// Recursively replaces all layers identified by oldLayer in the subtree 
		/// starting at the specified root game object with the layer specified by 
		/// newLayer.
		/// </summary>
		/// <param name='root'>The root game object of the subtree.</param>
		/// <param name='oldLayer'>The name of the replaced old layer.</param>
		/// <param name='newLayer'>The name of the new layer.</param>
		static public void ReplaceLayer(GameObject root, string oldLayer, string newLayer) {
			if(root.layer==LayerMask.NameToLayer(oldLayer))
				root.layer=LayerMask.NameToLayer(newLayer);

			foreach(Transform child in root.transform)
				ReplaceLayer(child.gameObject,oldLayer,newLayer);
		}
		
		/// <summary>
		/// Adds a MeshCollider component to the specified game object obj and
		/// uses the mesh of the object's MeshFilter in the new mesh collider.
		/// <para>If the specified game object already has a collider attached or 
		/// the game object does not contain a mesh (in a MeshFilter component)
		/// nothing is done. This function does no error logging.</para>
		/// </summary>
		/// <param name='obj'>The game object to add a mesh collider to.</param>
		static public void AddMeshCollider(GameObject obj){
			AddMeshCollider(obj,true);
		}

		/// <summary>
		/// Adds a MeshCollider component to the specified game object obj.
		/// <para>If the specified game object already has a collider attached or 
		/// the game object does not contain a mesh (in a MeshFilter component)
		/// nothing is done. If the specified verbose parameter is true, erros 
		/// will be logged to the console.</para>
		/// </summary>
		/// <param name='obj'>The game object to add a mesh collider to.</param>
		/// <param name='verbose'>If true error messages will be logged to the 
		/// console. If false, no error messages will be logged to the console.
		/// </param>
		static public void AddMeshCollider(GameObject obj, bool verbose){
			if(obj.GetComponent<Collider>()){
				if(verbose)
					Debug.LogError("Object already has a collider!");
				return;
			}
			MeshFilter mf=obj.GetComponent<MeshFilter>();
			if(!mf||!mf.sharedMesh){
				if(verbose)
					Debug.LogError("Object doesn't have a mesh!");
				return;
			}
			MeshCollider mc=obj.AddComponent<MeshCollider>();
			mc.sharedMesh=mf.sharedMesh;
		}

		
		/// <summary>Add a mesh collider to a subtree (recursively).</summary>
		/// <param name='root'>The root game object of the subtree.</param>
		static public void AddMeshColliderRecursive(GameObject root){
			AddMeshCollider(root,false);
			foreach(Transform child in root.transform)
				AddMeshColliderRecursive(child.gameObject);
		}
		
		/// <summary>Set Layer recursively.</summary>
		/// <param name='root'>The root game object of the subtree.</param>
		static public void SetLayerRecursive(GameObject root, int layer){
			root.layer=layer;
			foreach(Transform child in root.transform)
				SetLayerRecursive(child.gameObject,layer);
		}
		
		/// <summary>Find tagged objects recursively (including inactive ones!)</summary>
		static public List<GameObject> FindGameObjectsWithTag(GameObject root, string tag, List<GameObject> collector=null){
			if(collector==null)
				collector=new List<GameObject>();
			if(root.tag==tag)
				collector.Add(root);
			foreach(Transform child in root.transform)
				collector=FindGameObjectsWithTag(child.gameObject,tag,collector);
			return collector;
		}

		static public Bounds GetRendererBounds(GameObject root){
			Bounds bounds=new Bounds();
			bool firsttime=true;
			foreach(Renderer r in root.GetComponentsInChildren<Renderer>()){
				if(firsttime){
					// need this workaround since there is no empty Bounds() object,
					// so encapsulating a Bounds() object which does not contain (0,0,0)
					// would give an incorrect result
					bounds=r.bounds;
					firsttime=false;
				}else
					bounds.Encapsulate(r.bounds);
			}
			return bounds;
		}

		static public float MapInterval(float val, float dstMin, float dstMax) {
			return MapInterval(val,dstMin,dstMax,true);
		}
			
		/// <summary>
		/// Maps the specified value val from the interval 0..1 
		/// to the interval defined by dstMin and dstMax.
		/// </summary>
		/// <returns>The value val mapped to the destination interval.</returns>
		/// <param name='val'>The value to map from the source to the destination
		/// interval.</param>
		/// <param name='dstMin'>The minimum value of the destination interval.</param>
		/// <param name='dstMax'>The maximum value of the destination interval.</param>
		/// <param name='clamp'>Clamp values outside [dstMin..dstMax] or not.</param>
		static public float MapInterval(float val, float dstMin, float dstMax, bool clamp) {
			if(clamp){
				if (val>=1) return dstMax;
				if (val<=0) return dstMin;
			}
			return dstMin + val * (dstMax-dstMin);
		}	
		
		static public float MapInterval(float val, float srcMin, float srcMax, float dstMin, float dstMax) {
			return MapInterval(val,srcMin,srcMax,dstMin,dstMax,true);
		}
			
		/// <summary>
		/// Maps the specified value val from the interval defined by srcMin and 
		/// srcMax to the interval defined by dstMin and dstMax.
		/// </summary>
		/// <returns>The value val mapped to the destination interval.</returns>
		/// <param name='val'>The value to map from the source to the destination
		/// interval.</param>
		/// <param name='srcMin'>The minimum value of the source interval.</param>
		/// <param name='srcMax'>The maximum value of the source interval.</param>
		/// <param name='dstMin'>The minimum value of the destination interval.</param>
		/// <param name='dstMax'>The maximum value of the destination interval.</param>
		/// <param name='clamp'>Clamp values outside [dstMin..dstMax] or not.</param>
		static public float MapInterval(float val, float srcMin, float srcMax, float dstMin, float dstMax, bool clamp) {
			if(clamp){
				if (val>=srcMax) return dstMax;
				if (val<=srcMin) return dstMin;
			}
			return dstMin + (val-srcMin) / (srcMax-srcMin) * (dstMax-dstMin);
		}	
		
		/// <summary>
		/// Recursively calls DontDestroyOnLoad() on a subtree, taking care of
		/// (e.g. runtime generated) Materials, MeshColliders, and MeshFilters in the process.
		/// </summary>
		/// <param name='obj'>The root object.</param>
		static public void RecursiveDDOL(Object obj){
			Object.DontDestroyOnLoad(obj);

			//Debug.Log("found GO  "+((GameObject)obj).name);
			//Debug.Log(obj.name+" has "+((GameObject)obj).GetComponents<Component>().Length+" components");
			foreach(Component comp in ((GameObject)obj).GetComponents<Component>()){
				//Debug.Log("found CMP "+comp.GetType()+" "+((GameObject)obj).name);
				Object.DontDestroyOnLoad(comp);
				if(comp is Renderer){ // this is more convenient than comp.GetType().IsSubclassOf(typeof(Renderer))
					// Debug.Log("found Mat ");
					Object.DontDestroyOnLoad(((Renderer)comp).material);
				}else if(comp is MeshCollider){
					// Debug.Log("found MeshCollider "+(((MeshCollider)comp).sharedMesh!=null));
					Object.DontDestroyOnLoad(((MeshCollider)comp).sharedMesh);
				}else if(comp is MeshFilter){
					// Debug.Log("found MeshFilter "+(((MeshFilter)comp).mesh!=null));
					Object.DontDestroyOnLoad(((MeshFilter)comp).mesh);
				}
			}

			//Debug.Log("Walking through "+((GameObject)obj).transform.childCount+" children");
			foreach(Transform child in ((GameObject)obj).transform)
				RecursiveDDOL(child.gameObject);
		}
			
		static public void SetTextureColor(Texture2D texture, Color color){
			Color[] colorStrip = new Color[texture.width];
			for(int i=0;i<texture.width;i++)
				colorStrip[i]=color;
			for(int i=0;i<texture.height;i++)
				texture.SetPixels(0,i,texture.width,1,colorStrip);
			texture.Apply();
		}

		/// <summary>
		/// Sets the given color <paramref name="c"/> in the given material <paramref name="m"/>.
		/// This function first checks the material for the <c>_TintColor</c>, then the <c>_Emission</c> 
		/// and finally the <c>_Color</c> property and sets the given color <c>c</c> to the first
		/// match (in the given order).
		/// </summary>
		/// <param name="m">The material to set to the color.</param>
		/// <param name="c">The color to set.</param>
		static public void SetMaterialColor(Material m, Color c) {
			if (m.HasProperty("_TintColor"))
				m.SetColor("_TintColor", c*0.5f); // Unity's Particle shaders need half the actual color value (including alpha!)
			else if (m.HasProperty("_Emission")) {
				// TODO - this is probably no longer valid for the Standard shader...
				// Use the color's alpha in the "_Color" property of the "Transparent/Vertex Lit" shader...
				m.color = NMY.StaticUtils.ColorAlpha(Color.black, c.a);
				// the color itself must be set to the "_Emission" property
				m.SetColor("_Emission", c*0.5f);
			}else if (m.HasProperty("_EmisColor")) // builtin Particles/VertexLit Blended
				m.SetColor("_EmisColor", c*0.5f); // Unity's Particle shaders need half the actual color value (but for this one, alpha seems to be ignored...)
			else if (m.HasProperty("_Color"))
				m.color = c;
		}
			
		// NOTE: HasProperty() can check for the EXISTENCE, but not for the TYPE.
		//       So if Shaders use the names, but using float instead of Color, it will still
		//		 produce an error.
		//		 A workaround is in BnO - but it only works in the Editor...
		static public Color GetMaterialColor(Material m) {	
			if (m.HasProperty("_TintColor"))
				return m.GetColor("_TintColor")*2;
			else if (m.HasProperty("_Emission"))
				return m.GetColor("_Emission"); // TODO - see above
			else if (m.HasProperty("_EmisColor"))
				return m.GetColor("_EmisColor")*2;
			else if (m.HasProperty("_Color"))
				return m.color;
			else 
				return Color.white;
		}

		static public float GetMaterialAlpha(Material m) {	
			if (m.HasProperty("_Alpha"))
				return m.GetFloat("_Alpha");
			else 
				return GetMaterialColor(m).a;
		}
		
		// convenience method
		static public void SetMaterialAlpha(Material m, float alpha){
			if (m.HasProperty("_Alpha"))
				m.SetFloat("_Alpha",alpha);
			else
				SetMaterialColor(m,ColorAlpha(GetMaterialColor(m),alpha));
		}

		static public Material CreateUnlitMat(Color c, bool isGuiInFront) {
			return CreateUnlitMat(c, isGuiInFront, true);
		}

		static public Material CreateUnlitMat(Color c, bool isGuiInFront, bool useAlpha) {
			Material m;
			string alpha="";
			string gui="";

			if(useAlpha)
				alpha="Transparent/";
			if(isGuiInFront)
				gui=" (GUI, in front)";
				
			m = new Material(Shader.Find(alpha+"Unlit/Colored"+gui));
			SetMaterialColor(m, c);

			return m;
		}

		static public Texture2D CreateTexture(string filePath, TextureWrapMode wrapMode = TextureWrapMode.Clamp, FilterMode filterMode = FilterMode.Trilinear){
			Texture2D tex = new Texture2D(2, 2);

			if(File.Exists(filePath)){
				byte[] imageFile = File.ReadAllBytes(filePath);
				tex.LoadImage(imageFile);
				imageFile = null;

				tex.wrapMode = wrapMode;
				tex.filterMode = filterMode;
			}

			return tex;
		}

		static public Sprite CreateSprite(string filePath, TextureWrapMode wrapMode = TextureWrapMode.Clamp, FilterMode filterMode = FilterMode.Trilinear){
			Texture2D originalTexture = StaticUtils.CreateTexture(filePath, wrapMode, filterMode);
			return Sprite.Create(originalTexture, new Rect(Vector2.zero, new Vector2(originalTexture.width, originalTexture.height)), new Vector2(0.5f, 0.5f));
		}
			
		static public Shader GetTextShader(bool isInFront, bool isGUI) {
			string gui="";

			if (isInFront) {
				if (isGUI)
					gui = " (GUI, in front)";
				else
					gui = " (3D label, in front)";
			}

			return Shader.Find("GUI/Text Shader"+gui);
		}

		/// <summary>
		/// Change the alpha of the given color, either by replacing it (default multiply=false),
		/// or by weighting it (multiply=true).
		/// </summary>
		/// <param name="c">
		/// A <see cref="Color"/>
		/// </param>
		/// <param name="a">
		/// A <see cref="System.Single"/>
		/// </param>
		/// <param name="multiply">
		/// A <see cref="System.Boolean"/>
		/// </param>
		/// <returns>
		/// A <see cref="Color"/>
		/// </returns>
		static public Color ColorAlpha(Color c, float a, bool multiply=false){
			return new Color(c.r,c.g,c.b,multiply?c.a*a:a);
		}
			
		static public void ResetTransform(Transform tx, Transform parent) 
		{
			tx.SetParent(parent); // we don't care about the optional worldPositionStays flag; coord will be overwritten anyway
			ResetTransform(tx);
		}
			
		static public void ResetTransform(Transform tx) 
		{
			tx.localPosition = Vector3.zero;
			tx.localRotation = Quaternion.identity;
			tx.localScale = Vector3.one;
		}
			
		// convenience method to set a whole subtree to active=true/false ("SetActiveRecursively"),
		// but correctly taking care of embedded ActivatableStartupBehaviour objects
		// NOTE: two optional parameters of same type. per default, the first one will be changed. to change just the last one do:
		// StaticUtils.ActivateRecursively(myRoot, false, deactivateASBonly:true);
		// This sets activate to false, keeps immediately at false, and sets deactivateASBonly to true.
		static public bool ActivateRecursively(GameObject root, bool activate, bool immediately=false, bool deactivateASBonly=false){
			if(root)
				return ActivateRecursively(root.transform,activate,immediately, deactivateASBonly);
			return false;
		}
			
		// convenience method to set a whole subtree to active=true/false ("SetActiveRecursively"),
		// but correctly taking care of embedded ActivatableStartupBehaviour objects
		// NOTE: two optional parameters of same type. per default, the first one will be changed. to change just the last one do:
		// StaticUtils.ActivateRecursively(myRoot, false, deactivateASBonly:true);
		// This sets activate to false, keeps immediately at false, and sets deactivateASBonly to true.
		static public bool ActivateRecursively(Transform root, bool activate, bool immediately=false, bool deactivateASBonly=false){
			if(!root)
				return false;

			ActivatableStartupBehaviour[] asbs=root.GetComponents<ActivatableStartupBehaviour>();
			if(asbs.Length!=0){
				if(activate)
					root.gameObject.SetActive(true); // enable first!
				foreach(ActivatableStartupBehaviour asb in asbs){
//					Debug.Log(asb.name+" "+activate);
					if(immediately)
						asb.ActivateImmediately(activate);
					else
						asb.Activate(activate);
				}
				return true;
			}else{
				if(activate)
					root.gameObject.SetActive(true); // enable before descending

				bool childHasASB=false;
				foreach(Transform t in root.transform)
					childHasASB|=ActivateRecursively(t,activate,immediately);

				if(!activate && !childHasASB && !deactivateASBonly) // mustn't disable self if any child has an ASB, as this would disable the whole hierarchy immediately (>=4.0)
					root.gameObject.SetActive(false); // disable after returning
				return childHasASB;
			}
		}

		static public void SetAlphaRecursively(GameObject root, float alpha) {
			Color c;

			// MeshRenderer, LineRenderer, etc.
			foreach (Renderer mr in root.GetComponentsInChildren<Renderer>(true)) {
				foreach (Material m in mr.materials) {
					if(m.HasProperty("_TintColor")){
						c = m.GetColor("_TintColor");
						c.a = alpha/2; ///< alpha>0.5 has no effect on particles
						m.SetColor("_TintColor", c);
					}else if (m.HasProperty("_BaseColor")){ ///< URP
						c = m.GetColor("_BaseColor");
						c.a = alpha;
						m.SetColor("_BaseColor", c);
					}else if (m.HasProperty("_Color")){ ///< check this - shader might have no color setting at all
						c = m.color;
						c.a = alpha;
						m.color = c;
					}
				}
			}
		}
			
		static public string CommonRootPath(string path1, string path2) {
			string commonPath = "";
#if UNITY_STANDALONE
			string[] path1Dirs = path1.Split(new char[] {Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar});
			string[] path2Dirs = path2.Split(new char[] {Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar});
			
			int shortestPathLen = Mathf.Min(path1Dirs.Length, path2Dirs.Length);
			
			string tmpPath1 = "";
			string tmpPath2 = "";
			for (int i=0; i<shortestPathLen && commonPath==tmpPath1; i++) {
				tmpPath1 = Path.Combine(tmpPath1, path1Dirs[i]);
				tmpPath2 = Path.Combine(tmpPath2, path2Dirs[i]);
				// Path.Combine() will add '/' if necessary, but fails to do so if path1 is a volume.
				// So we just make sure there is always a '/'. This doesn't confuse Path.Combine().
				// In addition, this will make sure we end with a '/', which helps us to maintain relative pathes later on
				tmpPath1+=Path.DirectorySeparatorChar;
				tmpPath2+=Path.DirectorySeparatorChar;
				if (tmpPath1==tmpPath2)
					commonPath = tmpPath1;
			}
#endif
			return commonPath;
		}
		
		/// <summary>
		/// http://www.iandevlin.com/blog/2010/01/csharp/generating-a-relative-path-in-csharp
		/// </summary>	
//		public string RelativePath(string absPath, string relTo) {
//			string[] absDirs = absPath.Split('\\');
//			string[] relDirs = relTo.Split('\\');
//			
//			// Get the shortest of the two paths
//			int len = absDirs.Length < relDirs.Length ? absDirs.Length : 
//			relDirs.Length;
//			
//			// Use to determine where in the loop we exited
//			int lastCommonRoot = -1;
//			int index;
//			
//			// Find common root
//			for (index = 0; index < len; index++) {
//			if (absDirs[index] == relDirs[index]) lastCommonRoot = index;
//			else break;
//			}
//			
//			// If we didn't find a common prefix then throw
//			if (lastCommonRoot == -1) {
//			throw new ArgumentException("Paths do not have a common base");
//			}
//			
//			// Build up the relative path
//			StringBuilder relativePath = new StringBuilder();
//			
//			// Add on the ..
//			for (index = lastCommonRoot + 1; index < absDirs.Length; index++) {
//			if (absDirs[index].Length > 0) relativePath.Append("..\\");
//			}
//			
//			// Add on the folders
//			for (index = lastCommonRoot + 1; index < relDirs.Length - 1; index++) {
//			relativePath.Append(relDirs[index] + "\\");
//			}
//			relativePath.Append(relDirs[relDirs.Length - 1]);
//			
//			return relativePath.ToString();
//		}

		static public string RemoveNewLinesFromString(string text) {	
			if (string.IsNullOrEmpty(text)) return string.Empty;
			// fix stupid newline inconsistencies - yes, we need all these variants, in that order...
			string entryText=text;
			entryText=entryText.Replace("\r "," ");
			entryText=entryText.Replace("\r\n"," ");
			entryText=entryText.Replace("\r"," ");
			entryText=entryText.Replace("\n"," ");
				
			return entryText;
		}

		static public Bounds RecursiveMeshBB(GameObject go) {
			MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();

			if (mfs.Length>0) {
				Bounds b = mfs[0].mesh.bounds;
				for (int i=1; i<mfs.Length; i++) {
					b.Encapsulate(mfs[i].mesh.bounds);
				}
				return b;
			}
			else
				return new Bounds();
		}

		static public Bounds RecursiveRendererBB(GameObject go) {
			MeshRenderer[] mrs = go.GetComponentsInChildren<MeshRenderer>();

			if (mrs.Length>0) {
				Bounds b = mrs[0].bounds;
				for (int i=1; i<mrs.Length; i++) {
					b.Encapsulate(mrs[i].bounds);
				}
				return b;
			}
			else
				return new Bounds();
		}

		/// <summary>
		/// Gets the opposite of the given easeType
		/// </summary>
		static public iTween.EaseType GetInverseITweenEaseType(iTween.EaseType easeType){
			switch(easeType){
				case iTween.EaseType.easeInQuad:	return iTween.EaseType.easeOutQuad;
				case iTween.EaseType.easeOutQuad:	return iTween.EaseType.easeInQuad;
				case iTween.EaseType.easeInCubic:	return iTween.EaseType.easeOutCubic;
				case iTween.EaseType.easeOutCubic:	return iTween.EaseType.easeInCubic;
				case iTween.EaseType.easeInQuart:	return iTween.EaseType.easeOutQuart;
				case iTween.EaseType.easeOutQuart:	return iTween.EaseType.easeInQuart;
				case iTween.EaseType.easeInQuint:	return iTween.EaseType.easeOutQuint;
				case iTween.EaseType.easeOutQuint:	return iTween.EaseType.easeInQuint;
				case iTween.EaseType.easeInSine:	return iTween.EaseType.easeOutSine;
				case iTween.EaseType.easeOutSine:	return iTween.EaseType.easeInSine;
				case iTween.EaseType.easeInExpo:	return iTween.EaseType.easeOutExpo;
				case iTween.EaseType.easeOutExpo:	return iTween.EaseType.easeInExpo;
				case iTween.EaseType.easeInCirc:	return iTween.EaseType.easeOutCirc;
				case iTween.EaseType.easeOutCirc:	return iTween.EaseType.easeInCirc;
				case iTween.EaseType.easeInBounce:	return iTween.EaseType.easeOutBounce;
				case iTween.EaseType.easeOutBounce:	return iTween.EaseType.easeInBounce;
				case iTween.EaseType.easeInBack:	return iTween.EaseType.easeOutBack;
				case iTween.EaseType.easeOutBack:	return iTween.EaseType.easeInBack;
				case iTween.EaseType.easeInElastic:	return iTween.EaseType.easeOutElastic;
				case iTween.EaseType.easeOutElastic:return iTween.EaseType.easeInElastic;
				default:							return easeType;
			}
		}
		
		/// <summary>
		/// Simple iTween convenience method.
		/// </summary>
		static public void DoValueITween(GameObject targetGO, float time, float delay,
						     System.Action<object> onUpdate, // a lambda expression
						     iTween.EaseType easeType=iTween.EaseType.linear, // must be after required parameter onUpdate
						     System.Action<object, object> onComplete=null, // a lambda expression
							 bool ignoreTimeScale=false)
		{
			DoValueITween(targetGO,time,delay,0,1,onUpdate,easeType,onComplete,ignoreTimeScale);
		}

		/// <summary>
		/// Simple iTween convenience method.
		/// </summary>
		static public void DoValueITween(GameObject targetGO, float time, float delay,
						 float from, float to,
						 System.Action<object> onUpdate, // a lambda expression
						 iTween.EaseType easeType=iTween.EaseType.linear, // must be after required parameter onUpdate
						 System.Action<object, object> onComplete=null, // a lambda expression
						 bool ignoreTimeScale=false)
		{
			Hashtable args = new Hashtable();
			args.Add("time", time);
			args.Add("delay", delay);
			args.Add("from", from);
			args.Add("to", to);
			args.Add("ignoretimescale", ignoreTimeScale);
			args.Add("easetype", easeType);
			args.Add("onupdate", onUpdate);
			if(onComplete!=null)
				args.Add("oncomplete", onComplete);
			iTween.ValueTo(targetGO, args);
		}
		
		/// <summary>
		/// Compute Hamming distance of two binary values, with error tolerance.
		/// Determines whether two binary values are identical, within the given tolerance of bit errors
		/// </summary>
		static public bool IsSameID(int id1, int id2, int bitTolerance){
			int diff=id1^id2;
			int diffBits=CountBits(diff);
			return diffBits<=bitTolerance;
		}
			
		/// <summary>
		/// Compute Hamming distance of two binary values (=number of bits that differ)
		/// </summary>
		static public int GetHammingDistance(int id1, int id2){
			return CountBits(id1^id2);
		}
			
		/// <summary>
		/// Compute Hamming weight of a binary value (=count 1's)
		/// </summary>
		static public int CountBits(int val){
			int cnt=0;
			while(val!=0){
				val&=val-1; // optimized version for low number of set bits
				cnt++;
			}
			return cnt;
		}
	}

} // end namespace NMY
