﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class StartSceneEffectTextureEditor : SceneProcedure
{
	public StartSceneEffectTextureEditor(PROCEDURE_TYPE type, GameScene gameScene)
		:
	base(type, gameScene)
	{
		;
	}
	protected override void onInit(SceneProcedure lastProcedure, string intent)
	{
		LT.LOAD_NGUI_SHOW(LAYOUT_TYPE.LT_EFFECT_TEXTURE_EDITOR, 0);
	}
	protected override void onUpdate(float elapsedTime)
	{
		;
	}
	protected override void onExit(SceneProcedure nextProcedure)
	{
		LT.UNLOAD_LAYOUT(LAYOUT_TYPE.LT_EFFECT_TEXTURE_EDITOR);
	}
	protected override void onKeyProcess(float elapsedTime)
	{
		;
	}
}