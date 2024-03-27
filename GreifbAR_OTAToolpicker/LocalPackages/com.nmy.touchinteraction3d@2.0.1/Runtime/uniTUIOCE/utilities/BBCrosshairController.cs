using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// this object just handles the crosshairs that show up under each touch event

public class BBCrosshairController : MonoBehaviour {

	public GameObject crosshairPrefab;
	public float scale=1.0f;
	public GameObject markerPrefab;
	public float markerScale=1.0f;
	private float trueMarkerScale;
	private BBInputDelegate eventManager;
	
	private List<GameObject> crosshairs = new List<GameObject>();
	private List<GameObject> markers = new List<GameObject>();
	private int oldCrosshairIndex;
	private int oldMarkerIndex;
	private int oldScreenHeight;
	private int oldScreenWidth;

	private GameObject cursorCanvasGO;
	private Canvas cursorCanvas;

	void Start () {		
		eventManager = BBInputDelegate.instance;

		oldScreenWidth=Screen.width;
		oldScreenHeight=Screen.height;
		trueMarkerScale=markerScale*128/1080; // 100 pixels in a 128 px texture; 128 px texture on 1080 target screen height

		// check if we already have a canvas as the cameras child. If not, create one as we need it 
		// to display the crosshair cursor. The canvas is also used for marker visualisation (tangibles).
		cursorCanvas = GetComponentInChildren<Canvas>();
		if (cursorCanvas==null) {
			cursorCanvasGO = new GameObject("CursorCanvas");
			cursorCanvasGO.transform.SetParent(transform, true);
			cursorCanvas = cursorCanvasGO.AddComponent(typeof(Canvas)) as Canvas;
			cursorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
			cursorCanvas.sortingOrder = 99;
			UnityEngine.UI.CanvasScaler canvasScaler = cursorCanvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
			canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
			canvasScaler.matchWidthOrHeight = 1;
		}
	}
	
	void OnApplicationQuit() {
        eventManager = null;
    }
	
	// we go through each touch input and place a crosshair at it's position.
	// we save a list of crosshairs and deactivate them when they are not
	// being used.
	void Update () {
		int crosshairIndex = 0;
		int markerIndex = 0;
		if(eventManager.activeEvents.Count>0){
			try{
#if UNITY_FLASH
				foreach ( long flashFix in eventManager.activeEvents.Keys) {
					BBTouchEvent anEvent=eventManager.activeEvents[flashFix];
#else
				foreach ( BBTouchEvent anEvent in eventManager.activeEvents.Values) {
#endif
					if(anEvent.symbolID<0){
						if (crosshairs.Count <= crosshairIndex) {
							// make a new crosshair and cache it
							GameObject newCrosshair = (GameObject)Instantiate (crosshairPrefab, Vector3.zero, Quaternion.identity);
							// float width=newCrosshair.GetComponent<GUITexture>().pixelInset.width*scale;
							// float height=newCrosshair.GetComponent<GUITexture>().pixelInset.height*scale;
							newCrosshair.transform.SetParent(cursorCanvas.transform, false);
							newCrosshair.transform.localScale = new Vector3(scale, scale, scale);
							// newCrosshair.GetComponent<GUITexture>().pixelInset=new Rect(-width/2,-height/2,width,height);							
							crosshairs.Add(newCrosshair);
						}
						GameObject thisCrosshair = (GameObject)crosshairs[crosshairIndex];
						thisCrosshair.SetActive(true);
						if(anEvent.camera) {
							// Vector2 canvasSizeDelta = cursorCanvas.GetComponent<RectTransform>().sizeDelta;
							// Debug.Log("anEvent: lastScreenPos=" + anEvent.lastScreenPosition + ", normPos=" + anEvent.normPosition + ", canvas.anchorPos=" + cursorCanvas.GetComponent<RectTransform>().anchoredPosition);
							Vector3 pos = new Vector3(anEvent.normPosition.x*Screen.width, anEvent.normPosition.y*Screen.height, 0f);							
							thisCrosshair.transform.position = pos; // anEvent.camera.ScreenToViewportPoint(anEvent.screenPosition); // is different from anEvent.normPosition for non-fullscreen viewports
						}
						else {
							// thisCrosshair.transform.position = anEvent.normPosition;
							Debug.Log("anEvent: lastScreenPos=" + anEvent.lastScreenPosition + ", normPos=" + anEvent.normPosition);
							thisCrosshair.transform.position = anEvent.lastScreenPosition;
						}
						crosshairIndex++;
					}else if(markerPrefab!=null){
						if (markers.Count <= markerIndex) {
							// make a new marker and cache it
							GameObject newMarker = (GameObject)Instantiate (markerPrefab, Vector3.zero, Quaternion.identity);
							// newMarker.GetComponent<GUITexture>().pixelInset=new Rect(0,0,0,0);
							// newMarker.transform.localScale=new Vector3(Screen.height/(float)Screen.width*trueMarkerScale,trueMarkerScale,1);
							newMarker.transform.SetParent(cursorCanvas.transform, false);
							newMarker.transform.localScale = new Vector3(markerScale, markerScale, markerScale);
							markers.Add(newMarker);
						}
						GameObject thisMarker = (GameObject)markers[markerIndex];
#if TOUCHDEBUG
						// even more verbose:
						// get bit difference to best-fit valid marker
						int id=0;
						int hDist=256;
						for(int i=0;i<BBInputDelegate.instance.markerMapping.Count;i++) {
							if(NMY.StaticUtils.GetHammingDistance(BBInputDelegate.instance.markerMapping[i],anEvent.symbolID)<hDist) {
								hDist=NMY.StaticUtils.GetHammingDistance(BBInputDelegate.instance.markerMapping[i],anEvent.symbolID);
								id=i;
							}
						}
						thisMarker.GetComponent<GUIText>().text=System.Convert.ToString(BBInputDelegate.instance.markerMapping[id]^anEvent.symbolID,2)+"\n"+anEvent.symbolID.ToString()+"\n"+Mathf.RoundToInt(anEvent.tuioAngle*Mathf.Rad2Deg);
#else
						thisMarker.GetComponentInChildren<UnityEngine.UI.Text>().text=anEvent.symbolID.ToString()+"\n"+Mathf.RoundToInt(anEvent.tuioAngle*Mathf.Rad2Deg);
#endif
						thisMarker.SetActive(true);
						if(anEvent.camera) {
							Vector3 pos = new Vector3(anEvent.normPosition.x*Screen.width, anEvent.normPosition.y*Screen.height, 0f);
							thisMarker.transform.position = pos;
							// thisMarker.transform.position = anEvent.camera.ScreenToViewportPoint(anEvent.screenPosition); // is different from anEvent.normPosition for non-fullscreen viewports
						}
						else
							thisMarker.transform.position = anEvent.normPosition;
						markerIndex++;
					}
				}
			} catch {
				// silently ignore any out-of-sync errors (if eventManager.activeEvents changes with usePolling==false), as we really don't care
			}
		}

		// if the screen size changes (e.g., in the Editor), adjust cursor positions
		if(oldScreenHeight!=Screen.height || oldScreenWidth!= Screen.width){
			foreach(GameObject marker in markers)
				marker.transform.localScale=new Vector3(Screen.height/(float)Screen.width*trueMarkerScale,trueMarkerScale,1);
			oldScreenHeight=Screen.height;
			oldScreenWidth=Screen.width;
		}
			
		// if there are any extra ones, then shut them off
		for (int i = crosshairIndex; i < oldCrosshairIndex; i++)
			crosshairs[i].SetActive(false);
		oldCrosshairIndex=crosshairIndex;
		for (int i = markerIndex; i < oldMarkerIndex; i++)
			markers[i].SetActive(false);
		oldMarkerIndex=markerIndex;
	}
}
