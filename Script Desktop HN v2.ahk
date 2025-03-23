#SingleInstance
Persistent

SendMode "Input"

lockNum := 1
lockCaps := 1
SetNumlockState "AlwaysOn"
SetCapslockState "AlwaysOff"
SetScrolllockState "AlwaysOff"
osdOffTimer := -900
osdOffTimerFast := -400
	
;for general keyboard
;AppsKey::return
;AppsKey & Up::SendInput "{Volume_Up}"
;AppsKey & Down::SendInput "{Volume_Down}"
;AppsKey & Left::SendInput "{Media_Prev}"
;AppsKey & Right::SendInput "{Media_Next}"
;#`::SendInput "{Volume_Mute}"

;for mouse with side buttons
;XButton1::MButton							
;XButton2::Media_Play_Pause

;for Akko 5108S
Launch_App2::SendInput "{Media_Play_Pause}"

;custom command
#c::
{
	KeyWait "LWin"
	KeyWait "RWin"
	OpenCommandPrompt()
}
;#Space::return ;disable language selection

CloseGui(x)
{
	x.Destroy()
}

OpenCommandPrompt()
{
	fullpath := "C:\"
	for window in ComObject("Shell.Application").Windows
	{
		fullpath := window.Document.Folder.Self.Path
	}
    Run (A_ComSpec ' ' fullpath)
}

SetVolume(X)
{
	y:=x/2
	SendInput "{Volume_Down 51}"
	SendInput "{Volume_Up " y "}"
}

~*CapsLock::
{
	if (lockCaps = 1)
	{
		MyGui := Gui()
		MyGui.Opt("+ToolWindow -Caption +Border +AlwaysOnTop")
		MyGui.SetFont("s30")
		MyGui.AddText(,"Caps Lock Always OFF")
		MyGui.Show()
		t := CloseGui.Bind(MyGui)
		SetTimer t, osdOffTimerFast
	}	
}

#CapsLock::
{
	KeyWait "LWin"
	KeyWait "RWin"
	MyGui := Gui()
	MyGui.Opt("+ToolWindow -Caption +Border +AlwaysOnTop")
	MyGui.SetFont("s30")
	MyGui.AddText(,"Set Caps Lock")
	if (lockCaps = 1)
	{
		global lockCaps := 0
		SetCapslockState "On"
		MyGui.AddText(,"ON and unlocked")
	}
	else
	{
		global lockCaps := 1
		SetCapslockState "AlwaysOff"
		MyGui.AddText(,"Always OFF")
	}
	MyGui.Show()
	t := CloseGui.Bind(MyGui)
	SetTimer t, osdOffTimer
}

~*NumLock::
{
	if (lockNum = 1)
	{
		MyGui := Gui()
		MyGui.Opt("+ToolWindow -Caption +Border +AlwaysOnTop")
		MyGui.SetFont("s30")
		MyGui.AddText(,"Num Lock Always ON")
		MyGui.Show()
		t := CloseGui.Bind(MyGui)
		SetTimer t, osdOffTimerFast
	}
}

#NumLock::
{
	KeyWait "LWin"
	KeyWait "RWin"
	MyGui := Gui()
	MyGui.Opt("+ToolWindow -Caption +Border +AlwaysOnTop")
	MyGui.SetFont("s30")
	MyGui.AddText(,"Set Num Lock")
	if (lockNum = 1)
	{
		global lockNum := 0
		SetNumlockState "Off"
		MyGui.AddText(,"OFF and unlocked")
	}
	else
	{
		global lockNum := 1
		SetNumlockState "AlwaysOn"
		MyGui.AddText(,"Always ON")
	}
	MyGui.Show()		
	t := CloseGui.Bind(MyGui)
	SetTimer t, osdOffTimer
}