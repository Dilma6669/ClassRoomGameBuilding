using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws the selected creature's / pickup's hit zone and chase circles in the Scene view.
/// • Works even when the Scene view's Gizmos button is off.
/// • Works when you click the model (a child) instead of the creature itself.
/// Also switches the Scene view's Gizmos ON once each time Unity starts (the class can still turn them off).
/// </summary>
[InitializeOnLoad]
public static class ObstacleSceneDrawer
{
    static ObstacleSceneDrawer()
    {
        SceneView.duringSceneGui += OnScene;

        // Gizmos on by default (once per Unity session, so turning them off sticks until Unity restarts)
        if (!SessionState.GetBool("GameBuilder.GizmosOn", false))
        {
            EditorApplication.delayCall += () =>
            {
                foreach (SceneView view in SceneView.sceneViews) view.drawGizmos = true;
                if (SceneView.sceneViews.Count > 0) SessionState.SetBool("GameBuilder.GizmosOn", true);
            };
        }
    }

    private static void OnScene(SceneView view)
    {
        // Only with the Scene view's Gizmos button on (it's switched on by default)
        if (Event.current.type != EventType.Repaint || !view.drawGizmos) return;

        HashSet<ObstacleBase> drawn = new HashSet<ObstacleBase>();
        foreach (GameObject go in Selection.gameObjects)
        {
            if (go == null || !go.scene.IsValid()) continue;
            ObstacleBase o = go.GetComponentInParent<ObstacleBase>();
            if (o == null || !drawn.Add(o)) continue;
            DrawHitZone(o);
            DrawChase(o);
        }
    }

    private static void DrawHitZone(ObstacleBase o)
    {
        Transform zone = o.transform.Find(ObstacleBase.HitZoneName);
        if (zone == null) return;
        Transform body = zone.Find("Body");
        Transform touch = zone.Find("Touch");
        Color touchColour = o.payloadType == ObstacleBase.PayloadType.Damage ? new Color(1f, 0.25f, 0.2f) : new Color(0.3f, 1f, 0.4f);
        DrawPart(touch, touchColour, true);
        DrawPart(body, Color.white, false);
    }

    /// <summary>Draws one part of the hit zone: a wire ball for a Ball shape, else a wire cylinder.</summary>
    private static void DrawPart(Transform part, Color colour, bool onlyIfOn)
    {
        if (part == null) return;
        SphereCollider ball = part.GetComponent<SphereCollider>();
        if (ball != null && ball.enabled)
        {
            DrawBall(ball, colour);
            return;
        }
        MeshCollider mesh = part.GetComponent<MeshCollider>();
        if (onlyIfOn && (mesh == null || !mesh.enabled)) return;
        DrawCylinder(part, colour);
    }

    private static void DrawBall(SphereCollider ball, Color colour)
    {
        Vector3 centre = ball.transform.TransformPoint(ball.center);
        float r = ball.radius * Mathf.Max(ball.transform.lossyScale.x, ball.transform.lossyScale.y, ball.transform.lossyScale.z);
        Color oldColour = Handles.color;
        Handles.color = colour;
        Handles.DrawWireDisc(centre, Vector3.up, r, 2f);
        Handles.DrawWireDisc(centre, Vector3.right, r, 2f);
        Handles.DrawWireDisc(centre, Vector3.forward, r, 2f);
        Handles.color = oldColour;
    }

    /// <summary>A wire cylinder over Unity's built-in cylinder mesh (2 m wide, 2 m tall before scaling).</summary>
    private static void DrawCylinder(Transform part, Color colour)
    {
        MeshCollider mc = part.GetComponent<MeshCollider>();
        Mesh mesh = mc != null ? mc.sharedMesh : null;
        float r = mesh != null ? mesh.bounds.extents.x : 1f;
        float hh = mesh != null ? mesh.bounds.extents.y : 1f;

        Matrix4x4 old = Handles.matrix;
        Color oldColour = Handles.color;
        Handles.matrix = part.localToWorldMatrix;
        Handles.color = colour;
        Handles.DrawWireDisc(Vector3.up * hh, Vector3.up, r, 2f);
        Handles.DrawWireDisc(Vector3.down * hh, Vector3.up, r, 2f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            Vector3 side = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            Handles.DrawLine(side + Vector3.up * hh, side + Vector3.down * hh, 2f);
        }
        Handles.matrix = old;
        Handles.color = oldColour;
    }

    private static void DrawChase(ObstacleBase o)
    {
        if (!o.chasePlayer || o.payloadType != ObstacleBase.PayloadType.Damage) return;
        Color oldColour = Handles.color;
        Handles.color = new Color(1f, 0.6f, 0f);
        Handles.DrawWireDisc(o.transform.position, Vector3.up, o.chaseStartDistance, 2f);
        Handles.color = new Color(1f, 0.2f, 0.2f, 0.7f);
        Handles.DrawWireDisc(o.ChaseHome, Vector3.up, o.chaseGiveUpDistance, 2f);
        Handles.color = oldColour;
    }
}
