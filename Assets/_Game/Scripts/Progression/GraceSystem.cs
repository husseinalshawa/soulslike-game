using System.Collections.Generic;
using UnityEngine;

namespace SoulsGame.Progression
{
    // مواقع الراحة: اكتشاف بالاقتراب، جلوس عند المكتشف فقط،
    // وآخر موقع جلست عنده هو نقطة إحيائك.
    public class GraceSystem
    {
        public const float InteractRadius = 2f;

        private class Site
        {
            public int Id;
            public Vector3 Position;
            public bool Discovered;
        }

        private readonly List<Site> sites = new List<Site>();

        public Vector3 RespawnPosition { get; private set; }
        public int SiteCount => sites.Count;

        public void AddSite(int id, Vector3 position, bool discovered)
        {
            sites.Add(new Site { Id = id, Position = position, Discovered = discovered });
            if (sites.Count == 1)
                RespawnPosition = position;
        }

        // يكتشف أقرب موقع غير مكتشف ضمن النطاق ويعيد معرّفه، وإلا -1.
        public int TryDiscover(Vector3 playerPosition)
        {
            foreach (var site in sites)
            {
                if (site.Discovered)
                    continue;
                if (Vector3.Distance(playerPosition, site.Position) <= InteractRadius)
                {
                    site.Discovered = true;
                    return site.Id;
                }
            }
            return -1;
        }

        public bool CanRest(Vector3 playerPosition, out int siteId)
        {
            foreach (var site in sites)
            {
                if (site.Discovered && Vector3.Distance(playerPosition, site.Position) <= InteractRadius)
                {
                    siteId = site.Id;
                    return true;
                }
            }
            siteId = -1;
            return false;
        }

        public void RestAt(int siteId)
        {
            foreach (var site in sites)
                if (site.Id == siteId)
                {
                    RespawnPosition = site.Position;
                    return;
                }
        }

        public Vector3 PositionOf(int siteId)
        {
            foreach (var site in sites)
                if (site.Id == siteId)
                    return site.Position;
            return RespawnPosition;
        }
    }
}
