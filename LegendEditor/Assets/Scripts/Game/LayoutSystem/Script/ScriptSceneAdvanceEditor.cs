﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class PanelInfo
{
	public txNGUIPanel mPanel;
	public int mAtlasIndex;
	public int mObjectCount;
}

public class WindowSpritePool
{
	protected List<txNGUISprite> mInusedList;
	protected List<txNGUISprite> mUnusedList;
	protected LayoutScript mScript;
	public WindowSpritePool(LayoutScript script)
	{
		mScript = script;
		mInusedList = new List<txNGUISprite>();
		mUnusedList = new List<txNGUISprite>();
	}
	public txNGUISprite createWindow(string name)
	{
		txNGUISprite window = null;
		// 从未使用列表中获取
		if(mUnusedList.Count > 0)
		{
			window = mUnusedList[mUnusedList.Count - 1];
			mUnusedList.RemoveAt(mUnusedList.Count - 1);
		}
		// 未使用列表中没有就创建新窗口
		if(window == null)
		{
			window = mScript.createObject<txNGUISprite>(name);
		}
		// 加入到已使用列表中
		mInusedList.Add(window);
		window.setActive(true);
		return window;
	}
	public void destroyWindow(txNGUISprite window)
	{
		if(window == null)
		{
			return;
		}
		mUnusedList.Add(window);
		mInusedList.Remove(window);
		window.setActive(false);
	}
}

public class TileWindow
{
	public txNGUISprite mBackTile;
	public txNGUISprite mMidTile;
	public txNGUISprite mObjectTile;
	public void setActive(bool active)
	{
		if(mBackTile != null && mBackTile.isActive() != active)
		{
			mBackTile.setActive(active);
		}
		if (mMidTile != null && mMidTile.isActive() != active)
		{
			mMidTile.setActive(active);
		}
		if (mObjectTile != null && mObjectTile.isActive() != active)
		{
			mObjectTile.setActive(active);
		}
	}
}

public class ScriptSceneAdvanceEditor : LayoutScript
{
	protected txNGUIPanel mEditorRoot;
	protected txNGUIEditbox mSceneNameEdit;
	protected txNGUIButton mCreateNewScene;
	protected txNGUIText mWidthLabel;
	protected txNGUIText mHeightLabel;
	protected txNGUIEditbox mViewWidthEdit;
	protected txNGUIEditbox mViewHeightEdit;
	protected txNGUIText mViewWidthLabel;
	protected txNGUIText mViewHeightLabel;
	protected txUIObject mSceneRoot;
	protected txNGUIPanel mBackRoot;
	protected txUIObject mMiddleRoot;
	protected txNGUIPanel mObjRoot;
	protected Dictionary<int, PanelInfo> mBackPanelList;
	protected Dictionary<int, List<int>> mBackAtlasList;	// 根据图集下标查找有哪些panel包含该图集
	protected Vector2 mCurViewPos;						// 使用地图的坐标系,X轴向右,Y轴向下
	protected SceneMapAdvance mSceneMap;
	protected WindowSpritePool mWindowPool;
	protected TileWindow[] mTileArray;			// 当前场景所有的窗口
	protected Dictionary<int, MapTileAdvance> mVisibleTiles; // 存放当前可见的窗口
	protected UIAtlas midAtlas;
	protected Dictionary<int, Dictionary<int, UIAtlas>> mObjAtlasIndexMap;	// 存放所有使用过的地图对象图集,切换地图也不清空
	protected Dictionary<int, UIAtlas> mBngAtlasIndexMap;                     // 存放所有使用过的地图大地砖图集,切换地图也不清空
	protected string mBackTilePath;
	protected string mObjectImagePreString;
	protected Vector2 mHalfMap;
	protected const int MAX_WINDOW_PER_PANEL = 5000;
	protected int mMoveSpeed = 10;
	public ScriptSceneAdvanceEditor(string name, GameLayout layout)
		:
		base(name, layout)
	{
		mBackPanelList = new Dictionary<int, PanelInfo>();
		mBackAtlasList = new Dictionary<int, List<int>>();
		mWindowPool = new WindowSpritePool(this);
		mVisibleTiles = new Dictionary<int, MapTileAdvance>();
		mObjAtlasIndexMap = new Dictionary<int, Dictionary<int, UIAtlas>>();
		mBngAtlasIndexMap = new Dictionary<int, UIAtlas>();
		mBackTilePath = CommonDefine.R_ATLAS_PATH + "Map/Tiles/";
		mObjectImagePreString = CommonDefine.R_ATLAS_PATH + "Map/Objects";
	}
	public override void assignWindow()
	{
		newObject(out mEditorRoot, "EditorRoot");
		newObject(out mSceneNameEdit, mEditorRoot, "SceneNameEdit");
		newObject(out mCreateNewScene, mEditorRoot, "CreateNewScene");
		newObject(out mWidthLabel, mEditorRoot, "SceneWidth");
		newObject(out mHeightLabel, mEditorRoot, "SceneHeight");
		newObject(out mViewWidthEdit, mEditorRoot, "ViewWidthEdit");
		newObject(out mViewHeightEdit, mEditorRoot, "ViewHeightEdit");
		newObject(out mViewWidthLabel, mEditorRoot, "ViewWidthLabel");
		newObject(out mViewHeightLabel, mEditorRoot, "ViewHeightLabel");
		newObject(out mSceneRoot, "SceneRoot");
		newObject(out mBackRoot, mSceneRoot, "BackRoot");
		newObject(out mMiddleRoot, mSceneRoot, "MiddleRoot");
		newObject(out mObjRoot, mSceneRoot, "ObjRoot");
	}
	public override void init()
	{
		registeBoxColliderNGUI(mCreateNewScene, onCreateNewSceneClick);
		midAtlas = mResourceManager.loadResource<UIAtlas>(CommonDefine.R_ATLAS_PATH + "Map/SmTiles", true);
	}
	public override void onReset()
	{
		mWidthLabel.setLabel("宽:0");
		mHeightLabel.setLabel("高:0");
		mViewWidthEdit.setText("32");
		mViewHeightEdit.setText("32");
		mCurViewPos = Vector2.zero;
	}
	public override void onShow(bool immediately, string param)
	{
		;
	}
	public override void onHide(bool immediately, string param)
	{
		;
	}
	public override void update(float elapsedTime)
	{
		if(mSceneMap != null)
		{
			bool changed = false;
			Vector3 moveDelta = Vector3.zero;
			// 方向键用来移动地图
			if (getKeyCurrentUp(KeyCode.UpArrow))
			{
				mCurViewPos.y += 1 * mMoveSpeed;
				changed = true;
				moveDelta.y = 32 * mMoveSpeed;
			}
			if (getKeyCurrentUp(KeyCode.DownArrow))
			{
				mCurViewPos.y -= 1 * mMoveSpeed;
				changed = true;
				moveDelta.y = -32 * mMoveSpeed;
			}
			if (getKeyCurrentUp(KeyCode.LeftArrow))
			{
				mCurViewPos.x += 1 * mMoveSpeed;
				changed = true;
				moveDelta.x = -48 * mMoveSpeed;
			}
			if (getKeyCurrentUp(KeyCode.RightArrow))
			{
				mCurViewPos.x -= 1 * mMoveSpeed;
				changed = true;
				moveDelta.x = 48 * mMoveSpeed;
			}
			if (changed)
			{
				refreshMap();
				LayoutTools.MOVE_WINDOW(mSceneRoot, mSceneRoot.getPosition() + moveDelta);
			}
		}
	}
	public void refreshMap()
	{
		logInfo("view center : " + mCurViewPos, LOG_LEVEL.LL_FORCE);
		// 先判断是否有窗口已经从可见变为不可见
		int viewWidth = StringUtility.stringToInt(mViewWidthEdit.getText());
		int viewHeight = StringUtility.stringToInt(mViewHeightEdit.getText());
		List<int> removedIndex = new List<int>();
		foreach(var item in mVisibleTiles)
		{
			int x = tileIndexToTileX(item.Key);
			int y = tileIndexToTileY(item.Key);
			// 判断是否已经超出视野范围
			if (!MathUtility.isInRange(x, mCurViewPos.x - viewWidth / 2, mCurViewPos.x + viewWidth /2)
				|| !MathUtility.isInRange(y, mCurViewPos.y - viewHeight / 2, mCurViewPos.y + viewHeight / 2))
			{
				mTileArray[item.Key].setActive(false);
				removedIndex.Add(item.Key);
			}
		}
		int removedCount = removedIndex.Count;
		for(int i = 0; i < removedCount; ++i)
		{
			mVisibleTiles.Remove(removedIndex[i]);
		}
		// 查找从不可见变为可见的窗口
		for (int i = 0; i < viewWidth; ++i)
		{
			for (int j = 0; j < viewHeight; ++j)
			{
				int curTilePosX = i + (int)mCurViewPos.x - viewWidth / 2;
				int curTilePosY = j + (int)mCurViewPos.y - viewHeight / 2;
				int tileIndex = tilePosToTileIndex(curTilePosX, curTilePosY);
				if (MathUtility.isInRange(curTilePosX, (int)mCurViewPos.x - viewWidth / 2, (int)mCurViewPos.x + viewWidth / 2)
				&& MathUtility.isInRange(curTilePosY, (int)mCurViewPos.y - viewHeight / 2, (int)mCurViewPos.y + viewHeight / 2)
				&& MathUtility.isInRange(curTilePosX, 0, mSceneMap.mHeader.mWidth - 1)
				&& MathUtility.isInRange(curTilePosY, 0, mSceneMap.mHeader.mHeight - 1)
				&& !mVisibleTiles.ContainsKey(tileIndex))
				{
					// 变为可见的窗口如果不存在,则需要创建
					if (mTileArray[tileIndex] == null)
					{
						mTileArray[tileIndex] = new TileWindow();
						createTile(mTileArray[tileIndex], mSceneMap.mTileList[tileIndex]);
					}
					mTileArray[tileIndex].setActive(true);
					mVisibleTiles.Add(tileIndex, mSceneMap.mTileList[tileIndex]);
				}
			}
		}
	}
	public void createTile(TileWindow tileWindow, MapTileAdvance tile)
	{
		int x = tileIndexToTileX(tile.mIndex);
		int y = tileIndexToTileY(tile.mIndex);
		string tileSuffix = x + "_" + y;
		// 偶数行偶数列才渲染大地砖
		if (tileWindow.mBackTile == null && x % 2 == 0 && y % 2 == 0)
		{
			// 大地砖图集
			UIAtlas bngAtlas = getBackTileAtlas(tile);
			// 大地砖图片窗口
			string bngSpriteName = "" + (tile.mBngImgIdx - 1);
			if (bngAtlas != null && bngAtlas.GetSprite(bngSpriteName) != null)
			{
				if (!mBackAtlasList.ContainsKey(tile.mBngAtlasIndex))
				{
					mBackAtlasList.Add(tile.mBngAtlasIndex, new List<int>());
				}
				// 一个panel中只允许有一个Drawcall
				// 一个panel满时,就应该放入新的panel中
				// 查找该图集存放在哪些panel中
				int panelIndex = -1;
				List<int> panelIndexList = mBackAtlasList[tile.mBngAtlasIndex];
				int panelCount = panelIndexList.Count;
				for (int j = 0; j < panelCount; ++j)
				{
					// 找到一个包含该图集,且数量未达上限的panel
					if (mBackPanelList[panelIndexList[j]].mObjectCount < MAX_WINDOW_PER_PANEL)
					{
						panelIndex = panelIndexList[j];
						break;
					}
				}
				// 没有找到合适的panel,则创建新的panel
				if (panelIndex == -1)
				{
					panelIndex = GameUtility.makeID();
					PanelInfo panelInfo = new PanelInfo();
					panelInfo.mPanel = createObject<txNGUIPanel>(mBackRoot, "BackPanel" + panelIndex);
					panelInfo.mObjectCount = 0;
					panelInfo.mAtlasIndex = tile.mBngAtlasIndex;
					mBackPanelList.Add(panelIndex, panelInfo);
					mBackAtlasList[tile.mBngAtlasIndex].Add(panelIndex);
				}
				// panel中的窗口计数+1
				mBackPanelList[panelIndex].mObjectCount += 1;
				tileWindow.mBackTile = mWindowPool.createWindow("back_" + tileSuffix);
				tileWindow.mBackTile.setParent(mBackPanelList[panelIndex].mPanel);
				tileWindow.mBackTile.setAtlas(bngAtlas);
				tileWindow.mBackTile.setSpriteName(bngSpriteName, true);
				Vector2 posOffset = tileWindow.mBackTile.getSpriteSize() / 2.0f;
				posOffset += new Vector2(48 * x - mHalfMap.x, mHalfMap.y - 32 * y - 32);
				tileWindow.mBackTile.setLocalPosition(posOffset);
				tileWindow.mBackTile.setDepth(1);
			}
		}
		if(tileWindow.mMidTile == null)
		{
			// 小地砖图片窗口
			string midSpriteName = "" + (tile.mMidImgIdx - 1);
			if (midAtlas.GetSprite(midSpriteName) != null)
			{
				tileWindow.mMidTile = mWindowPool.createWindow("mid_" + tileSuffix);
				tileWindow.mMidTile.setParent(mMiddleRoot);
				tileWindow.mMidTile.setAtlas(midAtlas);
				tileWindow.mMidTile.setSpriteName(midSpriteName, true);
				Vector2 posOffset = tileWindow.mMidTile.getSpriteSize() / 2.0f;
				posOffset += new Vector2(48 * x - mHalfMap.x, mHalfMap.y - 32 * y);
				tileWindow.mMidTile.setLocalPosition(posOffset);
			}
		}
		if (tileWindow.mObjectTile == null)
		{
			// 加载对象图集
			UIAtlas atlas = getObjTileAtlas(tile);
			// 创建对象图片窗口
			string spriteName = "" + (tile.mObjImgIdx - 1);
			if (atlas != null && atlas.GetSprite(spriteName) != null)
			{
				tileWindow.mObjectTile = mWindowPool.createWindow("obj_" + tileSuffix);
				tileWindow.mObjectTile.setParent(mObjRoot);
				tileWindow.mObjectTile.setAtlas(atlas);
				tileWindow.mObjectTile.setSpriteName(spriteName, true);
				Vector2 posOffset = tileWindow.mObjectTile.getSpriteSize() / 2.0f;
				posOffset += new Vector2(48 * x - mHalfMap.x, mHalfMap.y - 32 * y);
				tileWindow.mObjectTile.setLocalPosition(posOffset);
				tileWindow.mObjectTile.setDepth(3);
			}
		}
	}
	public void clearSceneInfo()
	{
		foreach(var item in mBackPanelList)
		{
			destroyObject(item.Value.mPanel, true);
		}
		mBackPanelList.Clear();
		mBackAtlasList.Clear();
		mVisibleTiles.Clear();
		if(mTileArray != null)
		{
			int count = mTileArray.Length;
			for (int i = 0; i < count; ++i)
			{
				mWindowPool.destroyWindow(mTileArray[i].mBackTile);
				mWindowPool.destroyWindow(mTileArray[i].mMidTile);
				mWindowPool.destroyWindow(mTileArray[i].mObjectTile);
			}
		}
	}
	//------------------------------------------------------------------------------------------------------------------------------------
	protected void onCreateNewSceneClick(GameObject go)
	{
		clearSceneInfo();
		string mapFileName = CommonDefine.F_STREAMING_ASSETS_PATH + "Map/" + mSceneNameEdit.getText() + ".amap";
		mSceneMap = new SceneMapAdvance();
		mSceneMap.readFile(mapFileName);
		mWidthLabel.setLabel("宽:" + mSceneMap.mHeader.mWidth);
		mHeightLabel.setLabel("高:" + mSceneMap.mHeader.mHeight);
		mTileArray = new TileWindow[mSceneMap.mHeader.mWidth * mSceneMap.mHeader.mHeight];
		mHalfMap = new Vector2(mSceneMap.mHeader.mWidth * 48 / 2.0f, mSceneMap.mHeader.mHeight * 32 / 2.0f);
		mCurViewPos = new Vector2(mSceneMap.mHeader.mWidth / 2.0f, mSceneMap.mHeader.mHeight / 2.0f);
		refreshMap();
	}
	protected UIAtlas getBackTileAtlas(MapTileAdvance tile)
	{
		UIAtlas bngAtlas = null;
		if (tile.mBngAtlasIndex != 0xFF)
		{
			if (!mBngAtlasIndexMap.ContainsKey(tile.mBngAtlasIndex))
			{
				bngAtlas = mResourceManager.loadResource<UIAtlas>(mBackTilePath + tile.mBngAtlasIndex, true);
				mBngAtlasIndexMap.Add(tile.mBngAtlasIndex, bngAtlas);
			}
			else
			{
				bngAtlas = mBngAtlasIndexMap[tile.mBngAtlasIndex];
			}
		}
		return bngAtlas;
	}
	protected UIAtlas getObjTileAtlas(MapTileAdvance tile)
	{
		UIAtlas atlas = null;
		if (!mObjAtlasIndexMap.ContainsKey(tile.mObjFileIdx + 1))
		{
			mObjAtlasIndexMap.Add(tile.mObjFileIdx + 1, new Dictionary<int, UIAtlas>());
		}
		if (tile.mObjAtlasIndex != 0xFF)
		{
			if (!mObjAtlasIndexMap[tile.mObjFileIdx + 1].ContainsKey(tile.mObjAtlasIndex))
			{
				atlas = mResourceManager.loadResource<UIAtlas>(mObjectImagePreString + (tile.mObjFileIdx + 1) + "/" + tile.mObjAtlasIndex, true);
				mObjAtlasIndexMap[tile.mObjFileIdx + 1].Add(tile.mObjAtlasIndex, atlas);
			}
			else
			{
				atlas = mObjAtlasIndexMap[tile.mObjFileIdx + 1][tile.mObjAtlasIndex];
			}
		}
		return atlas;
	}
	protected int tileIndexToTileX(int index)
	{
		return index / mSceneMap.mHeader.mHeight;
	}
	protected int tileIndexToTileY(int index)
	{
		return index % mSceneMap.mHeader.mHeight;
	}
	protected int tilePosToTileIndex(int x, int y)
	{
		return x * mSceneMap.mHeader.mHeight + y;
	}
}