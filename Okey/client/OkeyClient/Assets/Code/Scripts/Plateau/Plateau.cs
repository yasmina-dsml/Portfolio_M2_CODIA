using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plateau : MonoBehaviour
{
    private string theme;

    private List<Chevalet> chevalets = new();
    private Stack<Tuile> pilePioche = new();
    private Tuile jocker;

    //Methode pour definir le themes
    public void SetTheme(string Type)
    {
        this.theme = Type;
    }

    //Methode pour obtenir la tuile jocker
    public Tuile GetJocker()
    {
        return this.jocker;
    }

    //methode pour definir la tuile joker
    public void SetJoker(Tuile JockerTile)
    {
        this.jocker = JockerTile;
    }

    // Methode pour depiler la pile pioche
    public List<Chevalet> GetChevalet()
    {
        return this.chevalets;
    }

    // Méthode pour initialiser le plateau de jeu
    public void InitialiserPlateau(
        string Theme,
        Stack<Tuile> Pioche,
        Tuile Joker,
        List<Chevalet> chevalets
    )
    {
        this.SetTheme(Theme);
        this.pilePioche = Pioche;
        this.SetJoker(Joker);
        this.chevalets = chevalets;
    }
}
