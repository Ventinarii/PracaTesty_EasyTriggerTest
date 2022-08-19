using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.gamescripts
{
    public interface Destructable
    {
        int GetHp();
        void SetHp(int value);

        /// <summary>
        /// check if bullet hit this object and apply damage / death animation if needed.
        /// </summary>
        /// <param name="location">location of bullet in LEVEL RELATIVE coords.</param>
        /// <param name="Damage">damage of bullet</param>
        /// /// <param name="punchSize">optional - if we punch how far do we reach?</param>
        /// <returns>true if bullet hit THIS Destructable</returns>
        bool IsHit(Vector2 location, int Damage, float punchSize = 0);
    }
}
