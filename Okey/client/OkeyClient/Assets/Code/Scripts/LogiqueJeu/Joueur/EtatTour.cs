namespace LogiqueJeu.Joueur
{
    using System;

    public class EtatTour : ICloneable
    {
        public enum Etats
        {
            Attente,
            Pioche,
            Pose,
            Fin
        }

        public Etats Etat { get; private set; }

        public EtatTour() => this.Etat = Etats.Attente;

        public Etats AvancerTour()
        {
            switch (this.Etat)
            {
                case Etats.Attente:
                    this.Etat = Etats.Pioche;
                    break;
                case Etats.Pioche:
                    this.Etat = Etats.Pose;
                    break;
                case Etats.Pose:
                    this.Etat = Etats.Fin;
                    break;
                case Etats.Fin:
                    throw new IndexOutOfRangeException(
                        "Fin de la tour, il n'est plus possible de l'avancer."
                    );
                default:
                    break;
            }
            return this.Etat;
        }

        public Etats ResetTour()
        {
            return this.Etat = Etats.Attente;
        }

        public object Clone()
        {
            return new EtatTour { Etat = this.Etat };
        }
    }
}
