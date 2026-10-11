"use client";

const Telecharger = () => {
    return(
        <div id="download" className="bg-green-800 text-white">
            <div className="mx-10 py-20 flex h-1/4">
                <div className="w-1/2 h-full flex flex-col justify-center">
                    <h1 className="font-bold text-4xl pb-5">Téléchargez OkeyGame</h1>
                    <p className="text-justify">Notre jeu est disponnible sur Desktop (Windows) et Mobile (Android). Installer le jeu sans s&lsquo;inscrire suffis pour pouvoir y jouer et rejoindre notre communauté !</p>
                </div>
                <div className="w-1/2 pt-3 flex flex-col justify-center mx-10">

                    <div className="pb-5 flex justify-between">
                        <button className="bg-primaryColor hover:bg-primaryColorHover text-black py-2 px-4 rounded">
                            Essayer sur Windows
                        </button>
                        <button className="bg-primaryColor hover:bg-primaryColorHover text-black py-2 px-4 rounded">
                          Essayer sur MacOS
                        </button>
                        <button className="bg-primaryColor hover:bg-primaryColorHover text-black py-2 px-4 rounded">
                          Essayer sur Linux
                        </button>
                    </div>

                    <button className="bg-primaryColor hover:bg-primaryColorHover text-black py-2 px-4 rounded">
                      Essayer sur Android
                    </button>
                </div>
            </div>
        </div>

    );

};

export default Telecharger;
