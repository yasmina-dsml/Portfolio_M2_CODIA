using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Tuile : MonoBehaviour
{
    [SerializeField]
    private string couleur;

    [SerializeField]
    private int valeur;
    private bool isJoker = false;
    private SpriteRenderer sprite;
    private bool deplacable = true;
    private bool estDeplace = false;
    private GameObject placeholderActuel;

    private GameObject PreviousPlaceHolder;
    private Vector3 offset;
    private Chevalet chevalet;
    private bool isInStack = false;
    private bool isInPioche = false;
    private Vector3 initialOffset;

    private void Start()
    {
        this.sprite = this.GetComponent<SpriteRenderer>();
        //chevalet = GetComponentInParent<Chevalet>(); // Find Chevalet script in the parent hierarchy
        this.chevalet = GameObject.Find("ChevaletFront").GetComponent<Chevalet>();
        this.placeholderActuel = this.transform.parent.gameObject;
    }

    private void Update()
    {
        if (this.estDeplace)
        {
            // Update tile position based on mouse position
            if (Camera.main != null)
            {
                var mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                this.transform.position = new Vector3(
                    mousePosition.x + this.initialOffset.x,
                    mousePosition.y + this.initialOffset.y,
                    -7
                );
            }
        }
    }

    private void OnMouseDown() //click
    {
        if (this.isInStack)
        {
            this.chevalet.Draw(true);
        }
        if (this.isInPioche)
        {
            this.chevalet.Draw(false);
        }
        if (this.deplacable)
        {
            // Save difference between mouse position and tile position
            if (Camera.main != null)
            {
                var mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                this.initialOffset = this.transform.position - mousePosition;
            }

            this.estDeplace = true;
        }
    }

    private void OnMouseUp() //release
    {
        if (this.deplacable)
        {
            this.estDeplace = false;
            if (Camera.main != null)
            {
                var mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                // Find the closest placeholder using the Chevalet's function
                var closestPlaceholder = this.chevalet.ClosestPlaceholder(
                    new Vector3(mousePosition.x, mousePosition.y, this.transform.localPosition.z)
                );
                //pour enregistrer le placeholder ou la tuile était avant que attach to placeholder modifie placeholderactuel
                this.PreviousPlaceHolder = this.placeholderActuel;

                if (closestPlaceholder != null)
                {
                    if (closestPlaceholder.transform.childCount == 0)
                    {
                        // Attach the tile to the new placeholder
                        if (closestPlaceholder != Chevalet.JokerPlaceHolder)
                        {
                            this.AttachToPlaceholder(closestPlaceholder);
                        }
                    }
                    else
                    {
                        if (closestPlaceholder != Chevalet.JokerPlaceHolder)
                        {
                            this.AttachToPlaceholder(closestPlaceholder);
                        }
                        else
                        {
                            this.AttachToPlaceholder(this.PreviousPlaceHolder);
                        }

                        if (
                            closestPlaceholder != Chevalet.PileDroitePlaceHolder
                            && closestPlaceholder != Chevalet.JokerPlaceHolder
                        )
                        {
                            // The placeholder countains already a Tile, we must update before insert
                            this.chevalet.UpdateTiles(closestPlaceholder);
                        }
                        else
                        {
                            this.deplacable = false;
                            if (closestPlaceholder == Chevalet.PileDroitePlaceHolder)
                            {
                                this.chevalet.ThrowTile(
                                    this.PreviousPlaceHolder.GetComponent<Tuile>()
                                );
                            }
                            else
                            {
                                this.deplacable = true;
                                Debug.LogWarning(
                                    $"On envoie le message {this.PreviousPlaceHolder.GetComponent<Tuile>()}"
                                );
                                this.chevalet.ThrowTileToWin(
                                    this.PreviousPlaceHolder.GetComponent<Tuile>()
                                );
                            }
                        }
                    }
                }
                Debug.Log($"{this.PreviousPlaceHolder} {closestPlaceholder}");
                this.chevalet.UpdateMatrixAfterMovement(
                    this.PreviousPlaceHolder,
                    closestPlaceholder
                ); //placeholder ou la piece était avant le deplacement et le placeholder ou elle a été deplacé
                if (closestPlaceholder != null)
                {
                    Debug.Log(
                        "Tile changed position from : "
                            + this.PreviousPlaceHolder.name
                            + " to "
                            + closestPlaceholder.name
                    );
                }
            }
        }
    }

    public void AttachToPlaceholder(GameObject placeholder)
    {
        // Detach tile from current parent (if any)
        var transform1 = this.transform;
        transform1.parent = null;

        // Set tile position to placeholder position
        transform1.localPosition = placeholder.transform.position;

        // Set tile rotation to placeholder rotation
        transform1.rotation = placeholder.transform.rotation;

        // Attach tile to the placeholder
        transform1.parent = placeholder.transform;
        this.placeholderActuel = placeholder;
    }

    /*
    public void SetBlockSprite(Sprite sprite)
    {
        this.sprite.sprite = sprite;
        this.couleur = sprite.name.Split('_')[0];
        this.valeur = int.Parse(sprite.name.Split('_')[1]);
    }*/

    public string GetCouleur()
    {
        return this.couleur;
    }

    public void SetCouleur(string coul)
    {
        this.couleur = coul;
    }

    public int GetValeur()
    {
        return this.valeur;
    }

    public void SetTileData(TuileData t)
    {
        this.SetCouleur(t.couleur);
        this.SetIsJoker(t.isJoker);
        this.SetValeur(t.num);
    }

    public void SetValeur(int val)
    {
        this.valeur = val;
    }

    public bool GetIsJoker()
    {
        return this.isJoker;
    }

    public void SetIsJoker(bool isJoke)
    {
        this.isJoker = isJoke;
    }

    public void SetIsDeplacable(bool isDeplacable)
    {
        this.deplacable = isDeplacable;
    }

    public void SetIsInStack(bool isInStack)
    {
        this.isInStack = isInStack;
    }

    public void SetIsInPioche(bool isInPioche)
    {
        this.isInPioche = isInPioche;
    }
}
