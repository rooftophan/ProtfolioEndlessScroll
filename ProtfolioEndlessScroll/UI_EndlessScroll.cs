using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_EndlessScroll : MonoBehaviour
{
#pragma warning disable 649

	[SerializeField] float _itempGap;

	[SerializeField] UIPanel _scrollPanel;
	[SerializeField] Transform _scrollParent;

	[SerializeField] GameObject _itemBase;

#pragma warning restore 649

	public Action<int, UIResources> _onLoadedItem;
	public Action<int> _onUnloadedItem;

	int _startItemIndex;
	int _endItemIndex;

	UIScrollView _scrollView;
	Vector3 _preScrollViewPos;

	[HideInInspector]
	public List<EndlessItemData> _endlessItemList = new List<EndlessItemData>();

	Queue<GameObject> _cachedObjects = new Queue<GameObject>();

	bool _isMoveScroll = false;
	bool _isEndlessState = false;

	float _itemStartPos;

	private void Awake() {
		if( _itemBase != null ) {
			_itemBase.SetActiveX( false );
		}
	}

	void LateUpdate() {
		if( !_isEndlessState )
			return;

		if( !_isMoveScroll )
			return;

		float gapValue = _scrollView.transform.localPosition.x - _preScrollViewPos.x;
		if( gapValue > 0f ) { // right
			RefreshRight();
		} else if( gapValue < 0f ){ // left
			RefreshLeft();
		}
	}

	public void RefreshRight() {
		float endItemPos = GetItemPos( _endItemIndex );
		if( GetCheckEndPos( _endlessItemList[_endItemIndex]._itemSize + _endlessItemList[_endItemIndex]._expandSize ) < endItemPos ) {
			if( _startItemIndex > 0 ) {
				ReleaseItem( _endItemIndex );

				if( _startItemIndex < _endItemIndex ) {
					_endItemIndex--;
				}

				EndlessItemData loadItem = LoadItem( _startItemIndex - 1 );
				if( loadItem != null ) {
					EndlessItemData startItemData = _endlessItemList[_startItemIndex];
					float itemPosX = startItemData._itemPos.x - (_itempGap + (startItemData._itemSize * 0.5f) + (loadItem._itemSize * 0.5f) + loadItem._expandSize);
					loadItem._itemObject.transform.localPosition = new Vector3( itemPosX, 0f, 0f );
					loadItem._itemPos = loadItem._itemObject.transform.localPosition;
					_startItemIndex--;
				}
			}
		}

		_preScrollViewPos = _scrollView.transform.localPosition;
	}

	public void RefreshLeft() {
		float startItemPos = GetItemPos( _startItemIndex );
		if( GetCheckStartPos( _endlessItemList[_startItemIndex]._itemSize + _endlessItemList[_startItemIndex]._expandSize ) > startItemPos ) {
			if( _endItemIndex < _endlessItemList.Count - 1 ) {
				ReleaseItem( _startItemIndex );

				if( _startItemIndex < _endItemIndex ) {
					_startItemIndex++;
				}

				EndlessItemData loadItem = LoadItem( _endItemIndex + 1 );
				if( loadItem != null ) {
					EndlessItemData endItemData = _endlessItemList[_endItemIndex];
					float itemPosX = endItemData._itemPos.x + _itempGap + (endItemData._itemSize * 0.5f) + endItemData._expandSize + (loadItem._itemSize * 0.5f);
					loadItem._itemObject.transform.localPosition = new Vector3( itemPosX, 0f, 0f );
					loadItem._itemPos = loadItem._itemObject.transform.localPosition;
					_endItemIndex++;
				}
			}
		}

		_preScrollViewPos = _scrollView.transform.localPosition;
	}

	public void InitData() {
		_scrollView = _scrollPanel.GetComponent<UIScrollView>();
		_scrollView.onDragStarted = OnDragStart;
		_scrollView.onStoppedMoving = OnStoppedMoving;
		_scrollView.transform.localPosition = Vector3.zero;
		_preScrollViewPos = _scrollView.transform.localPosition;

		float startX = -(_scrollPanel.GetViewSize().x * 0.5f);

		_startItemIndex = 0;
		_endItemIndex = 0;
		for(int i = 0;i< _endlessItemList.Count;i++ ) {
			int itemIndex = i;
			EndlessItemData itemData = _endlessItemList[itemIndex];
			if(i == 0 ) {
				startX += (itemData._itemSize * 0.5f);
				_itemStartPos = startX;
			} else {
				EndlessItemData preData = _endlessItemList[itemIndex - 1];
				startX += (preData._itemSize * 0.5f) + ( itemData._itemSize * 0.5f);
			}
			
			if(startX > GetCheckEndPos( (_endlessItemList[itemIndex]._itemSize * 3f) ) ) {
				_endItemIndex = itemIndex - 1;
				_isEndlessState = true;
				break;
			}
			_endItemIndex = i;

			itemData._isLoaded = true;

			GameObject itemObj = Instantiate<GameObject>( _itemBase );
			itemObj.name = string.Format( "ItemName_{0}", i );
			itemObj.SetActiveX( true );
			itemObj.transform.SetParentEx( _scrollParent );

			itemObj.transform.localPosition = new Vector3( startX, 0f, 0f );
			itemData._itemPos = itemObj.transform.localPosition;

			itemData._itemObject = itemObj;
			UIResources itemRes = itemObj.GetComponent<UIResources>();

			_onLoadedItem?.Invoke( itemIndex, itemRes );

			startX += _itempGap;
		}
	}

	public float GetStartItemPos() {
		return _itemStartPos;
	}

	void OnDragStart() {
		_isMoveScroll = true;
	}

	void OnStoppedMoving() {
		_isMoveScroll = false;
		_preScrollViewPos = _scrollView.transform.localPosition;
	}

	float GetCheckStartPos( float itemSize ) {
		float startX = -(_scrollPanel.GetViewSize().x * 0.5f);
		return startX - itemSize;
	}

	float GetCheckEndPos( float itemSize ) {
		return (_scrollPanel.GetViewSize().x * 0.5f) + itemSize;
	}

	public void RefreshPosition() {
		float startX = _endlessItemList[_startItemIndex]._itemPos.x + _itempGap;
		for(int i = _startItemIndex + 1;i<= _endItemIndex; i++ ) {
			int itemIndex = i;
			EndlessItemData itemData = _endlessItemList[itemIndex];
			EndlessItemData preData = _endlessItemList[itemIndex - 1];
			startX += (preData._itemSize * 0.5f) + preData._expandSize + ( itemData._itemSize * 0.5f);

			itemData._itemObject.transform.localPosition = new Vector3( startX, 0f, 0f );
			itemData._itemPos = itemData._itemObject.transform.localPosition;

			startX += _itempGap;
		}
	}

	float GetItemPos(int itemIndex ) {
		EndlessItemData itemData = _endlessItemList[itemIndex];

		return itemData._itemPos.x + _scrollView.transform.localPosition.x;
	}

	void ReleaseItem( int itemIndex ) {
		if( itemIndex >= _endlessItemList.Count ) {
			return;
		}

		EndlessItemData itemData = _endlessItemList[itemIndex];
		itemData._itemObject.SetActiveX( false );
		_cachedObjects.Enqueue( itemData._itemObject );
		itemData._itemObject = null;
		itemData._isLoaded = false;

		_onUnloadedItem?.Invoke( itemIndex );
	}

	EndlessItemData LoadItem( int itemIndex ) {
		if( itemIndex >= _endlessItemList.Count ) {
			return null;
		}

		EndlessItemData itemData = _endlessItemList[itemIndex];

		if( _cachedObjects.Count > 0 ) {
			itemData._itemObject = _cachedObjects.Dequeue();
		} else {
			GameObject itemObj = Instantiate<GameObject>( _itemBase );
			itemObj.transform.SetParentEx( _scrollParent );
			itemData._itemObject = itemObj;
		}

		itemData._itemObject.name = string.Format( "ItemName_{0}", itemIndex );
		itemData._itemObject.SetActiveX( true );
		itemData._isLoaded = true;

		UIResources itemRes = itemData._itemObject.GetComponent<UIResources>();
		_onLoadedItem?.Invoke( itemIndex, itemRes );

		_scrollPanel.Refresh();

		return itemData;
	}

	public void RefreshPanel() {
		_scrollPanel.Refresh();
	}
}
