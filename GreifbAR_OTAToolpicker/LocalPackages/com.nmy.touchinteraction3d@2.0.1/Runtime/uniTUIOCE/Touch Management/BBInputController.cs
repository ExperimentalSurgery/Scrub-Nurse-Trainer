// 080625 Jorgen Skogmo / shiftcontrol.dk
// Minimal implementation of a tuioclient
// modified by BenBritten

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TUIO;

public enum BBCursorState {
	Add,
	Update,
	Remove
};

public class BBCursorEvent 
{
	//public TuioCursor cursor; // we don't need the complete info
	public long id; // sessionID
	public float x;
	public float y;
	public BBCursorState state;
	
	public BBCursorEvent(long _id, float _x, float _y,BBCursorState s) {
		//cursor = c;
		id=_id;
		x=_x;
		y=_y;
		state = s;
	}
}

public class BBObjectEvent : BBCursorEvent
{
	public int symbol; // symbolID
	public float a; // angle

	public BBObjectEvent(long _id, int _symbol, float _x, float _y,float _a,BBCursorState s) : base(_id,_x,_y,s){
		symbol=_symbol;
		a=_a;
	}
}

#if UNITY_STANDALONE
public class BBInputController : TuioListener {

	private TuioClient[] clients;
	private int _numClients = 1; // number of simultaneous connections (=users)
	public int numClients {
		get { return _numClients; }
		set {
			if(value<_numClients){
				// disconnect superfluous clients, keep array
				for(int i=value;i<_numClients;i++)
					disconnect(i);
			}else if(value>_numClients){
				// update array
				TuioClient[] newArray=new TuioClient[value];
				for(int i=0;i<_numClients;i++)
					newArray[i]=clients[i];
				clients=newArray;
				
				// connect new clients
				for(int i=_numClients;i<value;i++){
					clients[i] = new TuioClient(3333+i);
					connect(i);
				}
			}
			_numClients=value;
		}
	}		
	
	// false: original variant: asynchronous TUIO, pushing events
	// true: new variant: inputDelegate is polling events by himself
	public bool collectEvents = false;

	volatile public bool flipTUIO = false;
	public bool detectMarkers = false;

	// original variant
	public BBInputDelegate eventDelegate;
	private bool cursorDidChange;
	private bool objectDidChange;

	// new variant
	public List<BBCursorEvent> activeCursorEvents = new List<BBCursorEvent>();
	public List<BBObjectEvent> activeObjectEvents = new List<BBObjectEvent>();
	private object objectSync = new object();

	public BBInputController(){
		clients=new TuioClient[numClients];
		for(int i=0;i<numClients;i++)
			clients[i] = new TuioClient(3333+i);
		connectAll();
		cursorDidChange = false;
		objectDidChange = false;
	}

	public List<BBCursorEvent> getAndClearCursorEvents() {
		List<BBCursorEvent> bufferList;
		lock(objectSync) {
			bufferList = new List<BBCursorEvent>(activeCursorEvents);
			activeCursorEvents.Clear();
		}
		return bufferList;
	}
	
	public List<BBObjectEvent> getAndClearObjectEvents() {
		List<BBObjectEvent> bufferList;
		lock(objectSync) {
			bufferList = new List<BBObjectEvent>(activeObjectEvents);
			activeObjectEvents.Clear();
		}
		return bufferList;
	}
	
	public void connect(int id) {
		clients[id].addTuioListener(this);
		clients[id].connect();
	}

	public void connectAll()
	{
		for(int i=0;i<numClients;i++)
			connect(i);
	}
	
	public void disconnect(int id) 
	{
		clients[id].disconnect();
		clients[id].removeTuioListener(this);
	}

	public void disconnectAll()
	{
		for(int i=0;i<numClients;i++)
			disconnect(i);
	}
	
	public bool isConnected(int id)
	{
		return clients[id].isConnected();
	}
	
	public bool isConnectedAny()
	{
		for(int i=0;i<numClients;i++)
			if(isConnected(i))
				return true;
		return false;
	}
	
	public bool isLocalClient(int id)
	{
		return isConnected(id) && clients[id].haveLocalClient;
	}
	
	public int currentFrame(int id)
	{
		return clients[id].currentFrameNumber();		
	}
	
	public string getStatusString(int id)
	{
		return clients[id].getStatusString();		
	}
	

	// marker objects, ie tangibles
	public void addTuioObject(TuioObject o) {
		if(collectEvents){
			lock(objectSync) {
				if(flipTUIO)
					activeObjectEvents.Add(new BBObjectEvent(o.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,o.getSymbolID(),1-o.getX(),1-o.getY(),o.getAngle()+Mathf.PI,BBCursorState.Add));
				else
					activeObjectEvents.Add(new BBObjectEvent(o.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,o.getSymbolID(),o.getX(),o.getY(),o.getAngle(),BBCursorState.Add));
			}
		}else{
			objectDidChange = true;
			if (eventDelegate)
				if(flipTUIO)
					eventDelegate.objectAdd(o.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,o.getSymbolID(),1-o.getX(),1-o.getY(),o.getAngle()+Mathf.PI);
				else
					eventDelegate.objectAdd(o.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,o.getSymbolID(),o.getX(),o.getY(),o.getAngle());
		}
	}
	
	public void updateTuioObject(TuioObject o) {
		if(collectEvents){
			lock(objectSync) {
				if(flipTUIO)
					activeObjectEvents.Add(new BBObjectEvent(o.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,o.getSymbolID(),1-o.getX(),1-o.getY(),o.getAngle()+Mathf.PI,BBCursorState.Update));
				else
					activeObjectEvents.Add(new BBObjectEvent(o.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,o.getSymbolID(),o.getX(),o.getY(),o.getAngle(),BBCursorState.Update));
			}
		}else{
			objectDidChange = true;
			if (eventDelegate)
				if(flipTUIO)
					eventDelegate.objectUpdate(o.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,o.getSymbolID(),1-o.getX(),1-o.getY(),o.getAngle()+Mathf.PI);
				else
					eventDelegate.objectUpdate(o.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,o.getSymbolID(),o.getX(),o.getY(),o.getAngle());
		}
	}
	
	public void removeTuioObject(TuioObject o) {
		if(collectEvents){
			lock(objectSync) {
				if(flipTUIO)
					activeObjectEvents.Add(new BBObjectEvent(o.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,o.getSymbolID(),1-o.getX(),1-o.getY(),o.getAngle()+Mathf.PI,BBCursorState.Remove));
				else
					activeObjectEvents.Add(new BBObjectEvent(o.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,o.getSymbolID(),o.getX(),o.getY(),o.getAngle(),BBCursorState.Remove));
			}
		}else{
			objectDidChange = true;
			if (eventDelegate)
				if(flipTUIO)
					eventDelegate.objectRemove(o.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,o.getSymbolID(),1-o.getX(),1-o.getY(),o.getAngle()+Mathf.PI);
				else
					eventDelegate.objectRemove(o.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,o.getSymbolID(),o.getX(),o.getY(),o.getAngle());
		}
	}

	// cursor objects, ie touch events
	public void addTuioCursor(TuioCursor c) {
		if(collectEvents){
			lock(objectSync) {
				if(flipTUIO)
					activeCursorEvents.Add(new BBCursorEvent(c.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,1-c.getX(),1-c.getY(),BBCursorState.Add));
				else
					activeCursorEvents.Add(new BBCursorEvent(c.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,c.getX(),c.getY(),BBCursorState.Add));
			}
		}else{
			cursorDidChange = true;
			if (eventDelegate)
				if(flipTUIO)
					eventDelegate.cursorDown(c.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,1-c.getX(),1-c.getY());
				else
					eventDelegate.cursorDown(c.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,c.getX(),c.getY());
		}
	}

	public void updateTuioCursor(TuioCursor c) {
		if(collectEvents){
			lock(objectSync) {
				if(flipTUIO)
					activeCursorEvents.Add(new BBCursorEvent(c.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,1-c.getX(),1-c.getY(),BBCursorState.Update));
				else
					activeCursorEvents.Add(new BBCursorEvent(c.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,c.getX(),c.getY(),BBCursorState.Update));
			}
		}else{
			cursorDidChange = true;
			if (eventDelegate)
				if(flipTUIO)
					eventDelegate.cursorMove(c.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,1-c.getX(),1-c.getY());
				else
					eventDelegate.cursorMove(c.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,c.getX(),c.getY());
		}
	}

	public void removeTuioCursor(TuioCursor c) {
		if(collectEvents){
			lock(objectSync) {
				if(flipTUIO)
					activeCursorEvents.Add(new BBCursorEvent(c.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,1-c.getX(),1-c.getY(),BBCursorState.Remove));
				else
					activeCursorEvents.Add(new BBCursorEvent(c.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,c.getX(),c.getY(),BBCursorState.Remove));
			}
		}else{
			cursorDidChange = true;
			if (eventDelegate)
				if(flipTUIO)
					eventDelegate.cursorUp(c.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,1-c.getX(),1-c.getY());
				else
					eventDelegate.cursorUp(c.getSessionID()+BBInputDelegate.STARTID_EXTERNAL,c.getX(),c.getY());
		}
	}
	
	// this is the end of a single frame
	public void refresh(TuioTime ftime) {
		if(!collectEvents){
			// we only really need to call the frame end if something actually happened this frame
			if (!cursorDidChange && !objectDidChange) return;
			if (eventDelegate) eventDelegate.finishFrame();	
			cursorDidChange = false;
			objectDidChange = false;
		}
		// else // we dont need to do anything here really
	}
}
#endif
