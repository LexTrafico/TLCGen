﻿#include "fixatie.h"

/*************************************************************************
 * konf_groen
 *************************************************************************
 * Bepaalt of een fasecyclus conflicterend groen heeft.
 */
bool konf_groen (count fc)
{
  register count i, j;

  for (i=0; i<GKFC_MAX[fc]; i++)
  {
   j = TO_pointer[fc][i];
   if (G[j]) return TRUE;
  }

  return FALSE;
}

/*************************************************************************
 * Fixatie
 *************************************************************************
 * Zorgt voor fixatie van het groenbeeld afhankelijk van ingang isFix
 *
 * Parameters:
 *    isFix        = index ingangssignaal fixatie keuze
 *    first, last  = (opeenvolgende) indici van richtingen die gefixeerd
 *                   dienen te worden
 *    bijkomen     = aanduiding of richtingen nog groen mogen worden
 *    prml         = primair of alternatieve toedeling van betreffende modulereeks
 *    ml           = huidige module in betreffende modulereeks
 *
 * Voorbeeld:
 *    Fixatie(isFix, 0, FCMAX-1, SCH[schbmfix], PRML, ML);
 *************************************************************************/
#if MLMAX
void Fixatie(count isFix, count first, count last, bool bijkomen, bool *prml[], count ml)
#else
void Fixatie(count isFix, count first, count last, bool bijkomen)
#endif
{
   register count fc;

   HTFB = CIF_IS[isFix];

   for (fc=first; fc<=last; fc++)
   {
      YV[fc]&= ~BIT0;
      YM[fc]&= ~BIT0;
      RR[fc]&= ~BIT0;
      if (CIF_IS[isFix])
      {
        YV[fc]|= BIT0;
        YM[fc]|= BIT0;
        if (bijkomen)
        {
          PAR[fc] = R[fc] && !TRG[fc] && !konf_groen(fc);
          RR[fc] &= ~BIT5; /* Reset 'normaal' alternatief terugzetten */
          RR[fc] |= !PAR[fc] ? BIT0 : 0;
          AA[fc] = PAR[fc] && !RR[fc] ? AA[fc] : FALSE;
        }
        else
          RR[fc]|= BIT0;
      }
   }
   if (bijkomen)
   {
     if (CIF_IS[isFix])
       langstwachtende_alternatief();
#if MLMAX
     for (fc=first; fc<=last; fc++)
     {
       if (AR[fc]) prml[ml][fc] |= ALTERNATIEF;
     }
#endif
   }
}