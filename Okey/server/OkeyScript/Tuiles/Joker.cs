namespace Okey.Tuiles
{
    public class Joker : Tuile
    {
        public Joker(CouleurTuile couleur, int valeur, bool dansPioche)
            : base(couleur, valeur, dansPioche)
        {
            //calculate value
        }

        public override bool MemeCouleur(Tuile t)
        {
            return this.Couleur == t.GetCouleur();
        }

        public override bool estSuivant(Tuile t)
        {
            return this.Num + 1 == t.GetNum() || (this.Num == 13 && t.GetNum() == 1);
        }

        public override String ToString()
        {
            return String.Format(null, "({0:00}, {1}, {2})", this.Num, this.Couleur, "Jo");
        }

        public override bool TuileEquals(Tuile t)
        {
            if (t is Okay)
                return true;
            return this.Num == t.GetNum() && this.Couleur == t.GetCouleur();
        }

        public override string GetName() => "Jo";
    }
}
