#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class BallMergeLevelEditorWindow
{
    public enum AlignOp
    {
        Left,
        CenterX,
        Right,
        Top,
        CenterY,
        Bottom
    }

    private void DrawCenterWorkspace()
    {
        Rect canvasRect = GUILayoutUtility.GetRect(10, 10, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (canvasRect.width > 50f && canvasRect.height > 50f)
        {
            lastCanvasRect = canvasRect;
        }

        Rect activeRect = lastCanvasRect.width > 50f ? lastCanvasRect : canvasRect;

        // Background
        EditorGUI.DrawRect(activeRect, new Color(0.11f, 0.12f, 0.16f, 1f));

        if (currentLevel == null)
        {
            GUI.Label(activeRect, "No Level Selected.\nSelect or create a level on the left.",
                new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 14 });
            return;
        }

        GUI.BeginClip(activeRect);
        try
        {
            Rect localBounds = new Rect(0f, 0f, activeRect.width, activeRect.height);
            HandleCanvasEvents(localBounds);
            DrawCanvasContents(localBounds);
        }
        finally
        {
            GUI.EndClip();
        }
    }

    #region Coordinate Conversions

    private Vector2 WorldToCanvas(Vector2 worldPos, Rect localBounds)
    {
        Vector2 origin = localBounds.size * 0.5f + panOffset;
        float ppu = BASE_PIXELS_PER_UNIT * zoom;
        return new Vector2(origin.x + worldPos.x * ppu, origin.y - worldPos.y * ppu);
    }

    private Vector2 CanvasToWorld(Vector2 canvasPos, Rect localBounds)
    {
        Vector2 origin = localBounds.size * 0.5f + panOffset;
        float ppu = BASE_PIXELS_PER_UNIT * zoom;
        return new Vector2((canvasPos.x - origin.x) / ppu, -(canvasPos.y - origin.y) / ppu);
    }

    private float WorldLengthToCanvas(float worldLen)
    {
        return worldLen * BASE_PIXELS_PER_UNIT * zoom;
    }

    private Vector2 SnapWorldPosition(Vector2 pos)
    {
        bool altFreeOverride = Event.current != null && Event.current.alt;
        if (!snapToGrid || altFreeOverride || gridSize <= 0.001f) return pos;

        return new Vector2(
            Mathf.Round(pos.x / gridSize) * gridSize,
            Mathf.Round(pos.y / gridSize) * gridSize
        );
    }

    private void SetZoom(float newZoom)
    {
        zoom = Mathf.Clamp(newZoom, 0.35f, 3.5f);
        Repaint();
    }

    private void ResetCanvasView()
    {
        zoom = 1.0f;
        panOffset = Vector2.zero;
        Repaint();
    }

    #endregion

    #region Canvas Rendering

    private void DrawCanvasContents(Rect localBounds)
    {
        float ppu = BASE_PIXELS_PER_UNIT * zoom;

        // 1. Grid
        if (showGrid)
        {
            DrawGrid(localBounds, ppu);
        }

        // 2. Mobile 9:16 Camera Viewport Preview (orthographicSize = 5.65 -> height 11.3, width ~6.35)
        if (showCameraFrame)
        {
            DrawCameraPreviewFrame(localBounds);
        }

        // 3. Container Boundaries (Floor, Left Wall, Right Wall)
        DrawContainerBounds(localBounds);

        // 4. Danger Line & Dropper Line
        DrawGameplayGuides(localBounds);

        // 5. Placed Blocks & Shelves
        DrawPlacedBlocks(localBounds);

        // 6. Selected Gizmos (Outline, Blast Reach, Resize Corners, Rotate Handle)
        DrawSelectionGizmos(localBounds);

        // 7. Brush Ghost Preview or Box Marquee
        DrawActiveToolOverlays(localBounds);

        // 8. Corner Status Bar inside Canvas
        DrawCanvasFooterOverlay(localBounds);
    }

    private void DrawGrid(Rect localBounds, float ppu)
    {
        float step = Mathf.Max(0.25f, gridSize);
        if (step * ppu < 12f) step *= 2f;
        if (step * ppu < 12f) step *= 2f;

        Vector2 worldMin = CanvasToWorld(new Vector2(0, localBounds.height), localBounds);
        Vector2 worldMax = CanvasToWorld(new Vector2(localBounds.width, 0), localBounds);

        Color minorCol = new Color(1f, 1f, 1f, 0.045f);
        Color majorCol = new Color(1f, 1f, 1f, 0.11f);
        Color axisCol = new Color(0.35f, 0.7f, 1f, 0.25f);

        float startX = Mathf.Floor(worldMin.x / step) * step;
        for (float x = startX; x <= worldMax.x; x += step)
        {
            float cx = WorldToCanvas(new Vector2(x, 0), localBounds).x;
            bool isAxis = Mathf.Abs(x) < 0.001f;
            bool isMajor = Mathf.Abs(x - Mathf.Round(x)) < 0.001f;
            Handles.color = isAxis ? axisCol : (isMajor ? majorCol : minorCol);
            Handles.DrawLine(new Vector3(cx, 0, 0), new Vector3(cx, localBounds.height, 0));
        }

        float startY = Mathf.Floor(worldMin.y / step) * step;
        for (float y = startY; y <= worldMax.y; y += step)
        {
            float cy = WorldToCanvas(new Vector2(0, y), localBounds).y;
            bool isFloor = Mathf.Abs(y - CONTAINER_FLOOR_Y) < 0.001f;
            bool isMajor = Mathf.Abs(y - Mathf.Round(y)) < 0.001f;
            Handles.color = isFloor ? axisCol : (isMajor ? majorCol : minorCol);
            Handles.DrawLine(new Vector3(0, cy, 0), new Vector3(localBounds.width, cy, 0));
        }
    }

    private void DrawCameraPreviewFrame(Rect localBounds)
    {
        float camHalfH = 5.65f;
        float camHalfW = camHalfH * (9f / 16f);

        Vector2 tl = WorldToCanvas(new Vector2(-camHalfW, camHalfH), localBounds);
        Vector2 br = WorldToCanvas(new Vector2(camHalfW, -camHalfH), localBounds);
        Rect camRect = Rect.MinMaxRect(tl.x, tl.y, br.x, br.y);

        Handles.color = new Color(0.55f, 0.75f, 1f, 0.35f);
        DrawDashedRect(camRect);
        GUI.Label(new Rect(camRect.x + 6, camRect.y + 4, 150, 18), "9:16 Mobile View", EditorStyles.miniLabel);
    }

    private void DrawContainerBounds(Rect localBounds)
    {
        float w = currentLevel.containerWidth;
        float h = currentLevel.containerHeight;
        float t = currentLevel.wallThickness;
        float halfW = w * 0.5f;
        float topY = CONTAINER_FLOOR_Y + h;

        Color wallCol = new Color(0.22f, 0.62f, 0.95f, 0.75f);

        // Interior highlight
        Vector2 intTL = WorldToCanvas(new Vector2(-halfW, topY), localBounds);
        Vector2 intBR = WorldToCanvas(new Vector2(halfW, CONTAINER_FLOOR_Y), localBounds);
        EditorGUI.DrawRect(Rect.MinMaxRect(intTL.x, intTL.y, intBR.x, intBR.y), new Color(0.16f, 0.19f, 0.27f, 0.45f));

        // Left wall
        DrawWorldRect(new Vector2(-halfW - t * 0.5f, CONTAINER_FLOOR_Y + h * 0.5f), new Vector2(t, h), wallCol, localBounds);
        // Right wall
        DrawWorldRect(new Vector2(halfW + t * 0.5f, CONTAINER_FLOOR_Y + h * 0.5f), new Vector2(t, h), wallCol, localBounds);
        // Bottom floor
        DrawWorldRect(new Vector2(0f, CONTAINER_FLOOR_Y - t * 0.5f), new Vector2(w + t * 2f, t), wallCol, localBounds);
    }

    private void DrawGameplayGuides(Rect localBounds)
    {
        float halfW = currentLevel.containerWidth * 0.5f;

        // Danger Line
        float dangerY = currentLevel.dangerLineY;
        Vector2 dL = WorldToCanvas(new Vector2(-halfW, dangerY), localBounds);
        Vector2 dR = WorldToCanvas(new Vector2(halfW, dangerY), localBounds);
        Handles.color = new Color(1f, 0.28f, 0.28f, 0.85f);
        Handles.DrawAAPolyLine(2.5f, new Vector3(dL.x, dL.y, 0f), new Vector3(dR.x, dR.y, 0f));
        GUI.Label(new Rect(dL.x + 6, dL.y - 16, 120, 16), $"Danger Line ({dangerY:0.0})",
            new GUIStyle(EditorStyles.miniBoldLabel) { normal = { textColor = new Color(1f, 0.45f, 0.45f) } });

        // Dropper Height
        float dropY = currentLevel.dropHeight;
        Vector2 drL = WorldToCanvas(new Vector2(-halfW, dropY), localBounds);
        Vector2 drR = WorldToCanvas(new Vector2(halfW, dropY), localBounds);
        Handles.color = new Color(0.4f, 0.95f, 0.65f, 0.55f);
        Handles.DrawAAPolyLine(2f, new Vector3(drL.x, drL.y, 0f), new Vector3(drR.x, drR.y, 0f));

        // Ghost Dropper Ball at X = 0
        Vector2 dropCenter = WorldToCanvas(new Vector2(0f, dropY), localBounds);
        float sampleRadiusPx = WorldLengthToCanvas(0.35f);
        Handles.color = new Color(0.95f, 0.35f, 0.35f, 0.65f);
        Handles.DrawWireDisc(dropCenter, Vector3.forward, sampleRadiusPx);
        GUI.Label(new Rect(dropCenter.x + sampleRadiusPx + 6, dropCenter.y - 8, 120, 18), $"Dropper ({dropY:0.0})",
            new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.55f, 0.95f, 0.7f) } });
    }

    private void DrawPlacedBlocks(Rect localBounds)
    {
        if (currentLevel.blocks == null) return;

        Vector2 mouseLocal = Event.current.mousePosition;
        int hoverEraserIdx = (currentTool == EditorTool.Erase && localBounds.Contains(mouseLocal))
            ? HitTestBlock(CanvasToWorld(mouseLocal, localBounds))
            : -1;

        for (int i = 0; i < currentLevel.blocks.Count; i++)
        {
            LevelBlockPlacement p = currentLevel.blocks[i];
            if (p == null) continue;

            BlockDefinitionSO def = p.ResolveDefinition(blockLibrary);
            Vector2 center = WorldToCanvas(p.position, localBounds);
            float wPx = WorldLengthToCanvas(p.size.x);
            float hPx = WorldLengthToCanvas(p.size.y);
            Rect blockRect = new Rect(center.x - wPx * 0.5f, center.y - hPx * 0.5f, wPx, hPx);

            // Optional attached shelf underneath
            if (p.addSupportShelf)
            {
                float shelfW = WorldLengthToCanvas(p.size.x + 0.35f);
                float shelfH = WorldLengthToCanvas(0.14f);
                Vector2 shelfOffsetLocal = RotateVec2(new Vector2(0f, -(p.size.y * 0.5f + 0.08f)), p.rotation);
                Vector2 shelfCenter = WorldToCanvas(p.position + shelfOffsetLocal, localBounds);
                Rect shelfRect = new Rect(shelfCenter.x - shelfW * 0.5f, shelfCenter.y - shelfH * 0.5f, shelfW, shelfH);

                Matrix4x4 mShelf = GUI.matrix;
                GUIUtility.RotateAroundPivot(-p.rotation, shelfCenter);
                EditorGUI.DrawRect(shelfRect, new Color(0.58f, 0.35f, 0.18f, 1f));
                GUI.matrix = mShelf;
            }

            // Block sprite or colored box (rotated around center)
            Color tint = p.useCustomColor ? p.customColor : (def != null ? def.baseColor : Color.white);
            if (i == hoverEraserIdx) tint = Color.Lerp(tint, Color.red, 0.65f);

            Matrix4x4 savedMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(-p.rotation, center);
            if (def != null && def.baseSprite != null)
            {
                DrawSpritePreviewInRect(blockRect, def.baseSprite, tint);
            }
            else
            {
                EditorGUI.DrawRect(blockRect, def != null ? def.accentColor : new Color(0.95f, 0.58f, 0.2f));
            }
            GUI.matrix = savedMatrix;

            // Special Behavior Canvas Overlays (Moving Path, Magnet Field, Key/Lock Badge)
            BlockBehaviorType bType = def != null ? def.behaviorType : BlockBehaviorType.Standard;
            bool isMoving = bType == BlockBehaviorType.MovingPlatform || p.overrideMovement;
            if (isMoving)
            {
                Vector2 moveOffset = p.GetEffectiveMoveOffset(def);
                if (moveOffset.sqrMagnitude > 0.001f)
                {
                    Vector2 endWorld = p.position + moveOffset;
                    Vector2 endCanvas = WorldToCanvas(endWorld, localBounds);
                    Handles.color = new Color(0.35f, 0.92f, 1f, 0.7f);
                    Handles.DrawDottedLine(new Vector3(center.x, center.y, 0f), new Vector3(endCanvas.x, endCanvas.y, 0f), 4f);
                    Rect ghostEnd = new Rect(endCanvas.x - wPx * 0.5f, endCanvas.y - hPx * 0.5f, wPx, hPx);
                    EditorGUI.DrawRect(ghostEnd, new Color(0.35f, 0.92f, 1f, 0.16f));
                }
            }

            if (bType == BlockBehaviorType.MagnetField)
            {
                float mRad = p.magnetRadiusOverride > 0.1f ? p.magnetRadiusOverride : (def != null ? def.magnetRadius : 2.4f);
                float mForce = Mathf.Abs(p.magnetForceOverride) > 0.01f ? p.magnetForceOverride : (def != null ? def.magnetForce : 8.5f);
                Handles.color = mForce >= 0f ? new Color(0.3f, 0.85f, 1f, 0.35f) : new Color(1f, 0.35f, 0.55f, 0.35f);
                Handles.DrawWireDisc(center, Vector3.forward, WorldLengthToCanvas(mRad));
            }

            if (bType == BlockBehaviorType.LockedGate && def != null && def.lockOverlaySprite != null)
            {
                float lockPx = Mathf.Clamp(Mathf.Min(wPx, hPx) * 0.55f, 14f, 34f);
                DrawSpritePreviewInRect(new Rect(center.x - lockPx * 0.5f, center.y - lockPx * 0.5f, lockPx, lockPx), def.lockOverlaySprite, Color.white);
            }

            // HP Badge / Static tag (drawn upright)
            if (showHPBadges)
            {
                if (bType == BlockBehaviorType.KeyTrigger || bType == BlockBehaviorType.LockedGate)
                {
                    int ch = p.GetEffectiveLockChannel(def);
                    string prefix = bType == BlockBehaviorType.KeyTrigger ? "K" : "L";
                    Rect chRect = new Rect(center.x + wPx * 0.5f - 20f, center.y - hPx * 0.5f, 20f, 15f);
                    EditorGUI.DrawRect(chRect, new Color(0.95f, 0.72f, 0.12f, 0.9f));
                    GUI.Label(chRect, $"{prefix}{ch}", new GUIStyle(EditorStyles.miniBoldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 9,
                        normal = { textColor = Color.black }
                    });
                }

                if (p.IsDestructible(def))
                {
                    int hp = p.GetEffectiveHP(def);
                    Rect pillRect = new Rect(center.x - 13, center.y - 10, 26, 20);
                    EditorGUI.DrawRect(pillRect, new Color(0.06f, 0.08f, 0.12f, 0.85f));
                    GUI.Label(pillRect, hp.ToString(), badgeStyle);
                }
                else
                {
                    string staticTag = bType == BlockBehaviorType.MovingPlatform ? "MOVE"
                                     : (bType == BlockBehaviorType.MagnetField ? "MAGNET"
                                     : (bType == BlockBehaviorType.BumperPad ? "BOUNCE" : "LEDGE"));
                    Rect tagRect = new Rect(center.x - 26, center.y - 8, 52, 16);
                    GUI.Label(tagRect, staticTag, new GUIStyle(EditorStyles.miniBoldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 9,
                        normal = { textColor = new Color(1f, 0.95f, 0.85f, 0.95f) }
                    });
                }
            }
        }

        DrawKeyLockConnections(localBounds);
    }

    private void DrawKeyLockConnections(Rect localBounds)
    {
        if (currentLevel.blocks == null) return;

        for (int i = 0; i < currentLevel.blocks.Count; i++)
        {
            LevelBlockPlacement keyBlk = currentLevel.blocks[i];
            if (keyBlk == null) continue;
            BlockDefinitionSO keyDef = keyBlk.ResolveDefinition(blockLibrary);
            if (keyDef == null || keyDef.behaviorType != BlockBehaviorType.KeyTrigger) continue;

            int keyChan = keyBlk.GetEffectiveLockChannel(keyDef);
            Vector2 keyPos = WorldToCanvas(keyBlk.position, localBounds);

            for (int j = 0; j < currentLevel.blocks.Count; j++)
            {
                LevelBlockPlacement lockBlk = currentLevel.blocks[j];
                if (lockBlk == null) continue;
                BlockDefinitionSO lockDef = lockBlk.ResolveDefinition(blockLibrary);
                if (lockDef == null || lockDef.behaviorType != BlockBehaviorType.LockedGate) continue;

                int lockChan = lockBlk.GetEffectiveLockChannel(lockDef);
                if (keyChan == 0 || lockChan == 0 || keyChan == lockChan)
                {
                    bool highlight = selectedIndices.Contains(i) || selectedIndices.Contains(j);
                    Handles.color = highlight ? new Color(1f, 0.85f, 0.2f, 0.95f) : new Color(1f, 0.85f, 0.2f, 0.35f);
                    Vector2 lockPos = WorldToCanvas(lockBlk.position, localBounds);
                    Handles.DrawDottedLine(new Vector3(keyPos.x, keyPos.y, 0f), new Vector3(lockPos.x, lockPos.y, 0f), 6f);
                }
            }
        }
    }

    private void DrawSelectionGizmos(Rect localBounds)
    {
        if (currentLevel.blocks == null || selectedIndices.Count == 0) return;

        foreach (int idx in selectedIndices)
        {
            if (idx < 0 || idx >= currentLevel.blocks.Count) continue;
            LevelBlockPlacement p = currentLevel.blocks[idx];
            if (p == null) continue;

            Vector2[] corners = GetBlockCanvasCorners(p, localBounds);
            Handles.color = new Color(0.25f, 0.9f, 1f, 1f);
            Handles.DrawAAPolyLine(2.5f,
                new Vector3(corners[0].x, corners[0].y, 0f),
                new Vector3(corners[1].x, corners[1].y, 0f),
                new Vector3(corners[2].x, corners[2].y, 0f),
                new Vector3(corners[3].x, corners[3].y, 0f),
                new Vector3(corners[0].x, corners[0].y, 0f));

            // Merge Blast Reach preview circle around selected destructible blocks
            if (showMergeRadiusPreview && p.IsDestructible(p.ResolveDefinition(blockLibrary)))
            {
                var def = p.ResolveDefinition(blockLibrary);
                float reach = currentLevel.mergeBlastRadius + (def != null ? def.extraMergeHitRadius : 0f);
                float reachPx = WorldLengthToCanvas(Mathf.Max(p.size.x, p.size.y) * 0.5f + reach);
                Vector2 c = WorldToCanvas(p.position, localBounds);
                Handles.color = new Color(1f, 0.82f, 0.2f, 0.35f);
                Handles.DrawWireDisc(c, Vector3.forward, reachPx);
            }
        }

        // Corner Resize & Rotate Handles for Single Selection
        if (selectedIndices.Count == 1)
        {
            int singleIdx = selectedIndices.First();
            if (singleIdx >= 0 && singleIdx < currentLevel.blocks.Count && currentLevel.blocks[singleIdx] != null)
            {
                LevelBlockPlacement p = currentLevel.blocks[singleIdx];
                Vector2[] corners = GetBlockCanvasCorners(p, localBounds);

                for (int c = 0; c < 4; c++)
                {
                    Rect handleRect = new Rect(corners[c].x - 5f, corners[c].y - 5f, 10f, 10f);
                    EditorGUI.DrawRect(handleRect, Color.white);
                    EditorGUI.DrawRect(new Rect(corners[c].x - 4f, corners[c].y - 4f, 8f, 8f), new Color(0.15f, 0.72f, 1f));
                }

                // Rotate Handle above block local top edge
                Vector2 topCenter = (corners[0] + corners[1]) * 0.5f;
                Vector2 rotHandlePos = GetRotateHandleCanvasPos(p, localBounds);
                Handles.color = new Color(1f, 0.8f, 0.25f, 0.95f);
                Handles.DrawAAPolyLine(2f, new Vector3(topCenter.x, topCenter.y, 0f), new Vector3(rotHandlePos.x, rotHandlePos.y, 0f));
                Handles.DrawSolidDisc(rotHandlePos, Vector3.forward, 6f);

                // Moving Platform Draggable Endpoint Handle
                BlockDefinitionSO def = p.ResolveDefinition(blockLibrary);
                if (p.overrideMovement || (def != null && def.behaviorType == BlockBehaviorType.MovingPlatform))
                {
                    Vector2 endCanvas = WorldToCanvas(p.position + p.GetEffectiveMoveOffset(def), localBounds);
                    Handles.color = new Color(0.25f, 0.95f, 0.85f, 1f);
                    Handles.DrawSolidDisc(endCanvas, Vector3.forward, 6.5f);
                    GUI.Label(new Rect(endCanvas.x + 8, endCanvas.y - 8, 70, 16), "END", EditorStyles.miniBoldLabel);
                }
            }
        }
    }

    private void DrawActiveToolOverlays(Rect localBounds)
    {
        Event e = Event.current;
        if (currentTool == EditorTool.Brush && activePaletteBlock != null && localBounds.Contains(e.mousePosition) && dragAction == DragAction.None)
        {
            Vector2 worldPos = SnapWorldPosition(CanvasToWorld(e.mousePosition, localBounds));
            Vector2 center = WorldToCanvas(worldPos, localBounds);
            float wPx = WorldLengthToCanvas(activePaletteBlock.defaultSize.x);
            float hPx = WorldLengthToCanvas(activePaletteBlock.defaultSize.y);
            Rect ghostRect = new Rect(center.x - wPx * 0.5f, center.y - hPx * 0.5f, wPx, hPx);

            if (activePaletteBlock.baseSprite != null)
            {
                DrawSpritePreviewInRect(ghostRect, activePaletteBlock.baseSprite, new Color(1f, 1f, 1f, 0.55f));
            }
            else
            {
                EditorGUI.DrawRect(ghostRect, new Color(1f, 0.8f, 0.2f, 0.45f));
            }
        }

        if (dragAction == DragAction.BoxSelect)
        {
            Rect box = Rect.MinMaxRect(
                Mathf.Min(dragStartCanvasPos.x, dragCurrentCanvasPos.x),
                Mathf.Min(dragStartCanvasPos.y, dragCurrentCanvasPos.y),
                Mathf.Max(dragStartCanvasPos.x, dragCurrentCanvasPos.x),
                Mathf.Max(dragStartCanvasPos.y, dragCurrentCanvasPos.y)
            );
            EditorGUI.DrawRect(box, new Color(0.25f, 0.75f, 1f, 0.16f));
            Handles.color = new Color(0.35f, 0.85f, 1f, 0.9f);
            Handles.DrawAAPolyLine(1.5f,
                new Vector3(box.xMin, box.yMin), new Vector3(box.xMax, box.yMin),
                new Vector3(box.xMax, box.yMax), new Vector3(box.xMin, box.yMax),
                new Vector3(box.xMin, box.yMin));
        }
    }

    private void DrawCanvasFooterOverlay(Rect localBounds)
    {
        Vector2 worldCursor = CanvasToWorld(Event.current.mousePosition, localBounds);
        string hint = $"Cursor: ({worldCursor.x:0.00}, {worldCursor.y:0.00})   |   Pan: Middle/Space+Drag   |   Zoom: Scroll   |   Free Place: Hold Alt   |   Q/B/E: Tools";
        Rect footerRect = new Rect(8, localBounds.height - 22, localBounds.width - 16, 18);
        EditorGUI.DrawRect(new Rect(0, localBounds.height - 24, localBounds.width, 24), new Color(0f, 0f, 0f, 0.45f));
        GUI.Label(footerRect, hint, EditorStyles.miniLabel);
    }

    private void DrawWorldRect(Vector2 worldCenter, Vector2 worldSize, Color col, Rect localBounds)
    {
        Vector2 tl = WorldToCanvas(worldCenter + new Vector2(-worldSize.x * 0.5f, worldSize.y * 0.5f), localBounds);
        Vector2 br = WorldToCanvas(worldCenter + new Vector2(worldSize.x * 0.5f, -worldSize.y * 0.5f), localBounds);
        EditorGUI.DrawRect(Rect.MinMaxRect(tl.x, tl.y, br.x, br.y), col);
    }

    private static void DrawDashedRect(Rect r)
    {
        Handles.DrawDottedLine(new Vector3(r.xMin, r.yMin), new Vector3(r.xMax, r.yMin), 5f);
        Handles.DrawDottedLine(new Vector3(r.xMax, r.yMin), new Vector3(r.xMax, r.yMax), 5f);
        Handles.DrawDottedLine(new Vector3(r.xMax, r.yMax), new Vector3(r.xMin, r.yMax), 5f);
        Handles.DrawDottedLine(new Vector3(r.xMin, r.yMax), new Vector3(r.xMin, r.yMin), 5f);
    }

    private static void DrawSpritePreviewInRect(Rect rect, Sprite sprite, Color tint)
    {
        if (sprite == null || sprite.texture == null) return;
        Rect tr = sprite.textureRect;
        Rect uvs = new Rect(
            tr.x / sprite.texture.width,
            tr.y / sprite.texture.height,
            tr.width / sprite.texture.width,
            tr.height / sprite.texture.height
        );
        Color prev = GUI.color;
        GUI.color = tint;
        GUI.DrawTextureWithTexCoords(rect, sprite.texture, uvs, true);
        GUI.color = prev;
    }

    #endregion

    #region Canvas Mouse, Drag-Drop & Keyboard Input

    private void HandleGlobalKeyboardShortcuts()
    {
        Event e = Event.current;
        if (e.keyCode == KeyCode.Space)
        {
            if (e.type == EventType.KeyDown && !EditorGUIUtility.editingTextField)
            {
                isSpaceHeld = true;
            }
            else if (e.type == EventType.KeyUp)
            {
                isSpaceHeld = false;
            }
        }

        if (e.type != EventType.KeyDown || EditorGUIUtility.editingTextField) return;

        bool ctrl = e.control || e.command;
        if (ctrl)
        {
            if (e.keyCode == KeyCode.C) { CopySelected(); e.Use(); return; }
            if (e.keyCode == KeyCode.V) { PasteClipboard(); e.Use(); return; }
            if (e.keyCode == KeyCode.D) { DuplicateSelected(); e.Use(); return; }
            if (e.keyCode == KeyCode.S) { SaveCurrentLevel(true); e.Use(); return; }
            if (e.keyCode == KeyCode.A && currentLevel != null && currentLevel.blocks != null)
            {
                selectedIndices.Clear();
                for (int i = 0; i < currentLevel.blocks.Count; i++) selectedIndices.Add(i);
                e.Use();
                Repaint();
                return;
            }
        }
        else
        {
            if (e.keyCode == KeyCode.Q) { currentTool = EditorTool.Select; e.Use(); Repaint(); return; }
            if (e.keyCode == KeyCode.B) { currentTool = EditorTool.Brush; e.Use(); Repaint(); return; }
            if (e.keyCode == KeyCode.E) { currentTool = EditorTool.Erase; e.Use(); Repaint(); return; }
            if (e.keyCode == KeyCode.Escape)
            {
                currentTool = EditorTool.Select;
                selectedIndices.Clear();
                e.Use();
                Repaint();
                return;
            }
            if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace)
            {
                DeleteSelected();
                e.Use();
                return;
            }

            // Arrow Key Nudging
            Vector2 nudge = Vector2.zero;
            float step = e.shift ? 0.5f : (snapToGrid ? gridSize : 0.05f);
            if (e.keyCode == KeyCode.LeftArrow) nudge = new Vector2(-step, 0);
            else if (e.keyCode == KeyCode.RightArrow) nudge = new Vector2(step, 0);
            else if (e.keyCode == KeyCode.UpArrow) nudge = new Vector2(0, step);
            else if (e.keyCode == KeyCode.DownArrow) nudge = new Vector2(0, -step);

            if (nudge != Vector2.zero && selectedIndices.Count > 0 && currentLevel != null && currentLevel.blocks != null)
            {
                RecordLevelUndo("Nudge Blocks");
                foreach (int idx in selectedIndices)
                {
                    if (idx >= 0 && idx < currentLevel.blocks.Count && currentLevel.blocks[idx] != null)
                    {
                        currentLevel.blocks[idx].position += nudge;
                    }
                }
                MarkCurrentLevelDirty();
                e.Use();
                Repaint();
            }
        }
    }

    private void HandleCanvasEvents(Rect localBounds)
    {
        Event e = Event.current;
        Vector2 localMouse = e.mousePosition;
        bool insideCanvas = localBounds.Contains(localMouse);

        // 1. Drag & Drop from Block Palette
        if (insideCanvas && (e.type == EventType.DragUpdated || e.type == EventType.DragPerform))
        {
            BlockDefinitionSO draggedDef = DragAndDrop.GetGenericData("BallMergeBlockDef") as BlockDefinitionSO;
            if (draggedDef == null && DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0)
            {
                draggedDef = DragAndDrop.objectReferences[0] as BlockDefinitionSO;
            }

            if (draggedDef != null)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (e.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    Vector2 dropWorld = SnapWorldPosition(CanvasToWorld(localMouse, localBounds));
                    PlaceNewBlock(draggedDef, dropWorld);
                    currentTool = EditorTool.Select;
                }
                e.Use();
                return;
            }
        }

        // 2. Scroll Wheel Zoom centered around cursor
        if (insideCanvas && e.type == EventType.ScrollWheel)
        {
            Vector2 worldBefore = CanvasToWorld(localMouse, localBounds);
            float delta = -e.delta.y * 0.05f;
            float newZoom = Mathf.Clamp(zoom * (1f + delta), 0.35f, 3.5f);
            if (!Mathf.Approximately(newZoom, zoom))
            {
                zoom = newZoom;
                Vector2 canvasAfter = WorldToCanvas(worldBefore, localBounds);
                panOffset += (localMouse - canvasAfter);
            }
            e.Use();
            Repaint();
            return;
        }

        // 3. Mouse Down
        if (insideCanvas && e.type == EventType.MouseDown)
        {
            GUIUtility.keyboardControl = 0; // Unfocus text fields cleanly when clicking canvas

            // Middle mouse or Alt+Left or Space+Left -> Pan
            if (e.button == 2 || (e.button == 0 && (e.alt || isSpaceHeld) && dragAction == DragAction.None))
            {
                dragAction = DragAction.Pan;
                dragStartCanvasPos = localMouse;
                panStartOffset = panOffset;
                e.Use();
                return;
            }

            // Right Click cancels Brush/Erase back to Select, or starts Pan
            if (e.button == 1)
            {
                if (currentTool != EditorTool.Select)
                {
                    currentTool = EditorTool.Select;
                    e.Use();
                    Repaint();
                }
                else
                {
                    dragAction = DragAction.Pan;
                    dragStartCanvasPos = localMouse;
                    panStartOffset = panOffset;
                    e.Use();
                }
                return;
            }

            if (e.button == 0)
            {
                Vector2 worldClick = CanvasToWorld(localMouse, localBounds);

                if (currentTool == EditorTool.Brush)
                {
                    if (activePaletteBlock != null)
                    {
                        PlaceNewBlock(activePaletteBlock, SnapWorldPosition(worldClick));
                    }
                    e.Use();
                    return;
                }

                if (currentTool == EditorTool.Erase)
                {
                    int hit = HitTestBlock(worldClick);
                    if (hit >= 0)
                    {
                        selectedIndices.Clear();
                        selectedIndices.Add(hit);
                        DeleteSelected();
                    }
                    e.Use();
                    return;
                }

                // Select Tool
                if (TryBeginSingleSelectionGizmo(localMouse, localBounds))
                {
                    e.Use();
                    return;
                }

                int hitIdx = HitTestBlock(worldClick);
                if (hitIdx >= 0)
                {
                    if (e.shift)
                    {
                        if (selectedIndices.Contains(hitIdx)) selectedIndices.Remove(hitIdx);
                        else selectedIndices.Add(hitIdx);
                    }
                    else if (!selectedIndices.Contains(hitIdx))
                    {
                        selectedIndices.Clear();
                        selectedIndices.Add(hitIdx);
                    }

                    BeginMoveSelectedBlocks(worldClick, hitIdx);
                    e.Use();
                    Repaint();
                    return;
                }

                // Empty space -> Box Marquee Select
                if (!e.shift) selectedIndices.Clear();
                dragAction = DragAction.BoxSelect;
                dragStartCanvasPos = localMouse;
                dragCurrentCanvasPos = localMouse;
                e.Use();
                Repaint();
            }
        }

        // 4. Mouse Drag
        if (e.type == EventType.MouseDrag && dragAction != DragAction.None)
        {
            Vector2 worldNow = CanvasToWorld(localMouse, localBounds);

            switch (dragAction)
            {
                case DragAction.Pan:
                    panOffset = panStartOffset + (localMouse - dragStartCanvasPos);
                    e.Use();
                    Repaint();
                    break;

                case DragAction.MoveBlocks:
                    UpdateMoveSelectedBlocks(worldNow);
                    e.Use();
                    Repaint();
                    break;

                case DragAction.ResizeCorner:
                    UpdateResizeBlock(worldNow);
                    e.Use();
                    Repaint();
                    break;

                case DragAction.RotateBlock:
                    UpdateRotateBlock(worldNow);
                    e.Use();
                    Repaint();
                    break;

                case DragAction.MovePathEnd:
                    UpdateMovePathEnd(worldNow);
                    e.Use();
                    Repaint();
                    break;

                case DragAction.BoxSelect:
                    dragCurrentCanvasPos = localMouse;
                    e.Use();
                    Repaint();
                    break;
            }
        }

        // 5. Mouse Up (check rawType so releasing outside canvas never leaves drag stuck)
        if ((e.type == EventType.MouseUp || e.rawType == EventType.MouseUp) && dragAction != DragAction.None)
        {
            if (dragAction == DragAction.BoxSelect)
            {
                FinalizeBoxSelection(localBounds);
            }
            else if (dragRecordedUndo)
            {
                MarkCurrentLevelDirty();
            }

            dragAction = DragAction.None;
            dragRecordedUndo = false;
            e.Use();
            Repaint();
        }
    }

    private void PlaceNewBlock(BlockDefinitionSO def, Vector2 worldPos)
    {
        if (currentLevel == null || def == null) return;
        RecordLevelUndo("Place Block");
        if (currentLevel.blocks == null) currentLevel.blocks = new List<LevelBlockPlacement>();

        LevelBlockPlacement placement = new LevelBlockPlacement
        {
            instanceGuid = Guid.NewGuid().ToString("N"),
            blockId = def.blockId,
            blockDefinition = def,
            position = worldPos,
            rotation = def.defaultRotation,
            size = def.defaultSize,
            hpOverride = 0,
            requiredToClear = def.requiredToClear
        };

        currentLevel.blocks.Add(placement);
        selectedIndices.Clear();
        selectedIndices.Add(currentLevel.blocks.Count - 1);
        MarkCurrentLevelDirty();
        Repaint();
    }

    private bool TryBeginSingleSelectionGizmo(Vector2 localMouse, Rect localBounds)
    {
        if (selectedIndices.Count != 1 || currentLevel.blocks == null) return false;
        int idx = selectedIndices.First();
        if (idx < 0 || idx >= currentLevel.blocks.Count) return false;
        LevelBlockPlacement p = currentLevel.blocks[idx];
        if (p == null) return false;

        // Moving Platform Endpoint Handle check
        BlockDefinitionSO def = p.ResolveDefinition(blockLibrary);
        if (p.overrideMovement || (def != null && def.behaviorType == BlockBehaviorType.MovingPlatform))
        {
            Vector2 endCanvas = WorldToCanvas(p.position + p.GetEffectiveMoveOffset(def), localBounds);
            if (Vector2.Distance(localMouse, endCanvas) <= 11f)
            {
                gizmoTargetIndex = idx;
                dragAction = DragAction.MovePathEnd;
                dragRecordedUndo = false;
                return true;
            }
        }

        // Rotate Handle check
        Vector2 rotPos = GetRotateHandleCanvasPos(p, localBounds);
        if (Vector2.Distance(localMouse, rotPos) <= 11f)
        {
            gizmoTargetIndex = idx;
            dragAction = DragAction.RotateBlock;
            dragRecordedUndo = false;
            return true;
        }

        // Corner Handles check: TL (-1,+1), TR (+1,+1), BR (+1,-1), BL (-1,-1)
        Vector2[] signs = { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, -1) };
        Vector2[] corners = GetBlockCanvasCorners(p, localBounds);
        for (int c = 0; c < 4; c++)
        {
            if (Vector2.Distance(localMouse, corners[c]) <= 10f)
            {
                gizmoTargetIndex = idx;
                resizeCornerSign = signs[c];
                resizeOrigRotation = p.rotation;
                resizeOrigSize = p.size;

                Vector2 oppLocal = new Vector2(-signs[c].x * p.size.x * 0.5f, -signs[c].y * p.size.y * 0.5f);
                resizeAnchorWorld = p.position + RotateVec2(oppLocal, p.rotation);
                dragAction = DragAction.ResizeCorner;
                dragRecordedUndo = false;
                return true;
            }
        }

        return false;
    }

    private void BeginMoveSelectedBlocks(Vector2 worldStart, int primaryIdx)
    {
        dragAction = DragAction.MoveBlocks;
        dragStartWorldPos = worldStart;
        movePrimaryIndex = primaryIdx;
        dragRecordedUndo = false;
        moveStartPositions.Clear();

        foreach (int idx in selectedIndices)
        {
            if (idx >= 0 && idx < currentLevel.blocks.Count && currentLevel.blocks[idx] != null)
            {
                moveStartPositions[idx] = currentLevel.blocks[idx].position;
            }
        }
    }

    private void UpdateMoveSelectedBlocks(Vector2 worldNow)
    {
        if (currentLevel.blocks == null || !moveStartPositions.ContainsKey(movePrimaryIndex)) return;

        if (!dragRecordedUndo)
        {
            RecordLevelUndo("Move Blocks");
            dragRecordedUndo = true;
        }

        Vector2 rawDelta = worldNow - dragStartWorldPos;
        Vector2 primarySnapped = SnapWorldPosition(moveStartPositions[movePrimaryIndex] + rawDelta);
        Vector2 finalDelta = primarySnapped - moveStartPositions[movePrimaryIndex];

        foreach (var kvp in moveStartPositions)
        {
            if (kvp.Key >= 0 && kvp.Key < currentLevel.blocks.Count && currentLevel.blocks[kvp.Key] != null)
            {
                currentLevel.blocks[kvp.Key].position = kvp.Value + finalDelta;
            }
        }
    }

    private void UpdateResizeBlock(Vector2 worldNow)
    {
        if (currentLevel.blocks == null || gizmoTargetIndex < 0 || gizmoTargetIndex >= currentLevel.blocks.Count) return;
        LevelBlockPlacement p = currentLevel.blocks[gizmoTargetIndex];
        if (p == null) return;

        if (!dragRecordedUndo)
        {
            RecordLevelUndo("Resize Block");
            dragRecordedUndo = true;
        }

        Vector2 localDelta = RotateVec2(worldNow - resizeAnchorWorld, -resizeOrigRotation);
        float newW = Mathf.Max(0.18f, localDelta.x * resizeCornerSign.x);
        float newH = Mathf.Max(0.12f, localDelta.y * resizeCornerSign.y);

        if (Event.current.shift && resizeOrigSize.y > 0.01f)
        {
            float aspect = resizeOrigSize.x / resizeOrigSize.y;
            newH = Mathf.Max(0.12f, newW / aspect);
        }

        if (snapToGrid && !Event.current.alt && gridSize > 0.01f)
        {
            newW = Mathf.Max(0.18f, Mathf.Round(newW / gridSize) * gridSize);
            newH = Mathf.Max(0.12f, Mathf.Round(newH / gridSize) * gridSize);
        }

        p.size = new Vector2(newW, newH);
        Vector2 newCenterLocal = new Vector2(resizeCornerSign.x * newW * 0.5f, resizeCornerSign.y * newH * 0.5f);
        p.position = resizeAnchorWorld + RotateVec2(newCenterLocal, resizeOrigRotation);
    }

    private void UpdateRotateBlock(Vector2 worldNow)
    {
        if (currentLevel.blocks == null || gizmoTargetIndex < 0 || gizmoTargetIndex >= currentLevel.blocks.Count) return;
        LevelBlockPlacement p = currentLevel.blocks[gizmoTargetIndex];
        if (p == null) return;

        if (!dragRecordedUndo)
        {
            RecordLevelUndo("Rotate Block");
            dragRecordedUndo = true;
        }

        Vector2 dir = worldNow - p.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        // Top handle points along local +Y, so angle from +Y is Atan2(-dir.x, dir.y)
        float angle = Mathf.Atan2(-dir.x, dir.y) * Mathf.Rad2Deg;
        if (snapToGrid || Event.current.shift)
        {
            angle = Mathf.Round(angle / 15f) * 15f;
        }
        p.rotation = Mathf.Repeat(angle + 180f, 360f) - 180f;
    }

    private void UpdateMovePathEnd(Vector2 worldNow)
    {
        if (currentLevel.blocks == null || gizmoTargetIndex < 0 || gizmoTargetIndex >= currentLevel.blocks.Count) return;
        LevelBlockPlacement p = currentLevel.blocks[gizmoTargetIndex];
        if (p == null) return;

        if (!dragRecordedUndo)
        {
            RecordLevelUndo("Edit Moving Platform Path");
            dragRecordedUndo = true;
        }

        Vector2 snappedEnd = SnapWorldPosition(worldNow);
        p.overrideMovement = true;
        p.moveOffset = snappedEnd - p.position;
    }

    private void FinalizeBoxSelection(Rect localBounds)
    {
        Vector2 minCanvas = new Vector2(Mathf.Min(dragStartCanvasPos.x, dragCurrentCanvasPos.x), Mathf.Min(dragStartCanvasPos.y, dragCurrentCanvasPos.y));
        Vector2 maxCanvas = new Vector2(Mathf.Max(dragStartCanvasPos.x, dragCurrentCanvasPos.x), Mathf.Max(dragStartCanvasPos.y, dragCurrentCanvasPos.y));

        if (Vector2.Distance(minCanvas, maxCanvas) < 5f || currentLevel.blocks == null) return;

        Vector2 worldMin = CanvasToWorld(new Vector2(minCanvas.x, maxCanvas.y), localBounds);
        Vector2 worldMax = CanvasToWorld(new Vector2(maxCanvas.x, minCanvas.y), localBounds);
        Rect worldSelRect = Rect.MinMaxRect(worldMin.x, worldMin.y, worldMax.x, worldMax.y);

        for (int i = 0; i < currentLevel.blocks.Count; i++)
        {
            var b = currentLevel.blocks[i];
            if (b != null && worldSelRect.Contains(b.position))
            {
                selectedIndices.Add(i);
            }
        }
    }

    private int HitTestBlock(Vector2 worldPos)
    {
        if (currentLevel == null || currentLevel.blocks == null) return -1;

        for (int i = currentLevel.blocks.Count - 1; i >= 0; i--)
        {
            LevelBlockPlacement p = currentLevel.blocks[i];
            if (p == null) continue;

            Vector2 local = RotateVec2(worldPos - p.position, -p.rotation);
            if (Mathf.Abs(local.x) <= p.size.x * 0.5f && Mathf.Abs(local.y) <= p.size.y * 0.5f)
            {
                return i;
            }
        }
        return -1;
    }

    #endregion

    #region Geometry, Align & Distribute

    private Vector2[] GetBlockCanvasCorners(LevelBlockPlacement p, Rect localBounds)
    {
        float hx = p.size.x * 0.5f;
        float hy = p.size.y * 0.5f;
        return new[]
        {
            WorldToCanvas(p.position + RotateVec2(new Vector2(-hx, hy), p.rotation), localBounds),
            WorldToCanvas(p.position + RotateVec2(new Vector2(hx, hy), p.rotation), localBounds),
            WorldToCanvas(p.position + RotateVec2(new Vector2(hx, -hy), p.rotation), localBounds),
            WorldToCanvas(p.position + RotateVec2(new Vector2(-hx, -hy), p.rotation), localBounds)
        };
    }

    private Vector2 GetRotateHandleCanvasPos(LevelBlockPlacement p, Rect localBounds)
    {
        Vector2 worldTop = p.position + RotateVec2(new Vector2(0f, p.size.y * 0.5f), p.rotation);
        Vector2 canvasTop = WorldToCanvas(worldTop, localBounds);
        Vector2 upDirCanvas = (WorldToCanvas(p.position + RotateVec2(Vector2.up, p.rotation), localBounds) - WorldToCanvas(p.position, localBounds)).normalized;
        return canvasTop + upDirCanvas * 22f;
    }

    private static Vector2 RotateVec2(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private void AlignSelected(AlignOp op)
    {
        if (currentLevel == null || currentLevel.blocks == null || selectedIndices.Count < 2) return;
        RecordLevelUndo($"Align {op}");

        var blocks = selectedIndices
            .Where(i => i >= 0 && i < currentLevel.blocks.Count && currentLevel.blocks[i] != null)
            .Select(i => currentLevel.blocks[i])
            .ToList();
        if (blocks.Count < 2) return;

        float minX = blocks.Min(b => b.position.x - b.size.x * 0.5f);
        float maxX = blocks.Max(b => b.position.x + b.size.x * 0.5f);
        float minY = blocks.Min(b => b.position.y - b.size.y * 0.5f);
        float maxY = blocks.Max(b => b.position.y + b.size.y * 0.5f);
        float midX = (minX + maxX) * 0.5f;
        float midY = (minY + maxY) * 0.5f;

        for (int i = 0; i < blocks.Count; i++)
        {
            var b = blocks[i];
            switch (op)
            {
                case AlignOp.Left: b.position.x = minX + b.size.x * 0.5f; break;
                case AlignOp.CenterX: b.position.x = midX; break;
                case AlignOp.Right: b.position.x = maxX - b.size.x * 0.5f; break;
                case AlignOp.Top: b.position.y = maxY - b.size.y * 0.5f; break;
                case AlignOp.CenterY: b.position.y = midY; break;
                case AlignOp.Bottom: b.position.y = minY + b.size.y * 0.5f; break;
            }
        }

        MarkCurrentLevelDirty();
        Repaint();
    }

    private void DistributeSelected(bool horizontal)
    {
        if (currentLevel == null || currentLevel.blocks == null || selectedIndices.Count < 3) return;
        RecordLevelUndo(horizontal ? "Distribute Horizontal" : "Distribute Vertical");

        var blocks = selectedIndices
            .Where(i => i >= 0 && i < currentLevel.blocks.Count && currentLevel.blocks[i] != null)
            .Select(i => currentLevel.blocks[i])
            .OrderBy(b => horizontal ? b.position.x : b.position.y)
            .ToList();
        if (blocks.Count < 3) return;

        float first = horizontal ? blocks[0].position.x : blocks[0].position.y;
        float last = horizontal ? blocks[blocks.Count - 1].position.x : blocks[blocks.Count - 1].position.y;

        for (int i = 1; i < blocks.Count - 1; i++)
        {
            float t = (float)i / (blocks.Count - 1);
            float v = Mathf.Lerp(first, last, t);
            if (horizontal) blocks[i].position.x = v;
            else blocks[i].position.y = v;
        }

        MarkCurrentLevelDirty();
        Repaint();
    }

    private void MirrorSelectedHorizontal()
    {
        if (currentLevel == null || currentLevel.blocks == null || selectedIndices.Count == 0) return;
        RecordLevelUndo("Mirror Horizontal");

        foreach (int idx in selectedIndices)
        {
            if (idx >= 0 && idx < currentLevel.blocks.Count && currentLevel.blocks[idx] != null)
            {
                var b = currentLevel.blocks[idx];
                b.position.x = -b.position.x;
                b.rotation = -b.rotation;
            }
        }

        MarkCurrentLevelDirty();
        Repaint();
    }

    #endregion
}
#endif
