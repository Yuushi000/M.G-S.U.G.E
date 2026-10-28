using System;
using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    [Serializable]
    public class BonePoseEntry
    {
        public string bone;
        public bool hasRot; public Quaternion rot = Quaternion.identity;
        public bool hasPos; public Vector3 pos;
        public bool hasScale; public float scale = 1f;
    }
    [Serializable]
    public class FramePose
    {
        public string name;
        public List<BonePoseEntry> bones = new List<BonePoseEntry>();
        public BonePoseEntry Get(string bone)
        {
            foreach (var b in bones) if (b.bone == bone) return b;
            return null;
        }
    }

    /// <summary>
    /// Poses de reference du chassis exportees de Blender (MGSUGE_Vesper_Poses.json) :
    /// decalages en espace modele (X droite, Y haut, Z avant) appliques a la pose de liaison.
    /// </summary>
    [CreateAssetMenu(menuName = "MGSUGE/Donnees/Poses du chassis", fileName = "MGSUGE_Poses")]
    public class PoseLibrary : ScriptableObject
    {
        public List<FramePose> poses = new List<FramePose>();
        public FramePose Get(string name)
        {
            foreach (var p in poses) if (p.name == name) return p;
            return null;
        }

        [Serializable] class JVec { public float x, y, z, w = 1f; }
        [Serializable] class JBone { public string bone; public bool hasRot, hasPos, hasScale; public JVec rot, pos; public float scale = 1f; }
        [Serializable] class JPose { public string name; public JBone[] bones; }
        [Serializable] class JRoot { public JPose[] poses; }

        public void LoadJson(string json)
        {
            var root = JsonUtility.FromJson<JRoot>(json);
            poses = new List<FramePose>();
            foreach (var jp in root.poses)
            {
                var p = new FramePose { name = jp.name };
                foreach (var jb in jp.bones)
                    p.bones.Add(new BonePoseEntry
                    {
                        bone = jb.bone,
                        hasRot = jb.hasRot, rot = jb.rot != null ? new Quaternion(jb.rot.x, jb.rot.y, jb.rot.z, jb.rot.w) : Quaternion.identity,
                        hasPos = jb.hasPos, pos = jb.pos != null ? new Vector3(jb.pos.x, jb.pos.y, jb.pos.z) : Vector3.zero,
                        hasScale = jb.hasScale, scale = jb.scale
                    });
                poses.Add(p);
            }
        }
    }
}
