"use client";

import React from "react";
import Image from "next/image";

const OkeyHistory = () => {
    return (
      <div className="flex h-1/4">
        <div className="w-1/2 h-full flex items-center justify-center">
          <img
            src="/imgs/history_pic.jpeg"
            alt="Image"
            className="h-full w-auto max-h-full max-w-full ml-8"
          />
        </div>
        <div className="w-1/2 p-8 flex flex-col justify-center">
          <h3 className="font-bold text-xl">Jouer en partie rapide avec notre communauté</h3>
          <p className="font-normal text-m">Nous avons des salles de jeux publiques pour pouvoir jouer contre des adversaires faisant preuve de stratégie et de vivacité d&lsquo;esprit.</p><br />
          <h3 className="font-bold text-xl">Créez ou rejoignez des salles de jeux privées</h3>
          <p className="font-normal text-m">Jouer entre amis ou avec votre famille en toute convivialité et avec l&lsquo;esprit de rivalité. </p><br />
          <h3 className="font-bold text-xl">Votre jeu sur Desktop et dans votre poche !</h3>
          <p className="font-normal text-m">Notre jeu est disponnible sur Windows, MacOS, Linux ainsi que Android.</p>
        </div>
      </div>
    );

};

export default OkeyHistory;
