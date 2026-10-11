"use client";

import React from "react";
const Tutoriel = () => {
    return(
        <div id="tutoriel" className="text-center py-32">
            <h3 className="text-3xl md:text-4xl font-bold">Apprendre à jouer</h3>
            <video className='max-w-full h-auto items-center md:h-80 mt-4 mx-auto' controls>
                Your browser does not support the video tag.
            </video>
        </div>
    );
};


export default Tutoriel;
