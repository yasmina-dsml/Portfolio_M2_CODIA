"use client";

import React, { useState, useEffect } from 'react';
import Image from 'next/image';

const Navbar = () => {
  const [isOpen, setIsOpen] = useState(false);
  const [scrolling, setScrolling] = useState(false);

  const toggleNavbar = () => {
    setIsOpen(!isOpen);
  };


  useEffect(() => {
    const handleScroll = () => {
      if (window.scrollY > 300) {
        setScrolling(true);
      } else {
        setScrolling(false);
      }
    };

    window.addEventListener('scroll', handleScroll);

    return () => {
      window.removeEventListener('scroll', handleScroll);
    };
  }, []);

  const changeLanguage = (language: string) => {
    // Here you would implement the logic to change the language
    console.log(`Changing language to ${language}`);
  };

  return (
    <nav className={`fixed w-full z-10 h-28 flex justify-center items-center ${scrolling ? 'bg-green-800' : 'bg-transparent'} backdrop-filter backdrop-blur-lg`}>
      <div className="max-w-7xl mx-auto px-4">
        <div className="flex justify-between h-full items-center">
          <div className="flex items-center">
            <Image src="/imgs/logo.png" alt="Logo" width={100} height={100} />
          </div>
          <div className="hidden md:block">
            <div className="ml-10 flex items-center space-x-4">
              <a href="#tutoriel" className="text-white px-3 py-2 rounded-md text-sm font-medium">Tutoriel</a>
              <a href="#download" className="text-white px-3 py-2 rounded-md text-sm font-medium">Télécharger le jeu</a>
              <div className="relative">
                <button onClick={toggleNavbar} className="text-white px-3 py-2 rounded-md text-sm font-medium focus:outline-none">Langue</button>
                {/* Ajout de l'état isOpen pour contrôler la visibilité du menu */}
                {isOpen && (
                  <div className="absolute right-0 mt-2 w-48 bg-green-800 rounded-md shadow-lg origin-top-right ring-1 ring-black ring-opacity-5 divide-y divide-gray-100 focus:outline-none" role="menu" aria-orientation="vertical" aria-labelledby="options-menu">
                    <a href="#" className="block px-4 py-2 text-sm text-white hover:bg-green-700" onClick={() => {changeLanguage('fr')}}>Français</a>
                    <a href="#" className="block px-4 py-2 text-sm text-white hover:bg-green-700" onClick={() => {changeLanguage('en')}}>English</a>
                    <a href="#" className="block px-4 py-2 text-sm text-white hover:bg-green-700" onClick={() => {changeLanguage('tr')}}>Türkçe</a>
                  </div>
                )}
              </div>
            </div>
          </div>
          <div className="-mr-2 flex md:hidden">
            <button onClick={toggleNavbar} className="inline-flex items-center justify-center p-2 rounded-md text-gray-400 hover:text-white hover:bg-gray-700 focus:outline-none focus:bg-gray-700 focus:text-white">
              <svg className={`${isOpen ? 'hidden' : 'block'} h-6 w-6`} xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 6h16M4 12h16m-7 6h7"></path>
              </svg>
              <svg className={`${isOpen ? 'block' : 'hidden'} h-6 w-6`} xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12"></path>
              </svg>
            </button>
          </div>
        </div>
      </div>

      <div className={`${isOpen ? 'block' : 'hidden'} md:hidden`}>
        <div className="px-2 pt-2 pb-3 space-y-1 sm:px-3">
          <a href="#" className="text-gray-300 hover:bg-gray-700 hover:text-white block px-3 py-2 rounded-md text-base font-medium">Home</a>
          <a href="#" className="text-gray-300 hover:bg-gray-700 hover:text-white block px-3 py-2 rounded-md text-base font-medium">About</a>
          <a href="#" className="text-gray-300 hover:bg-gray-700 hover:text-white block px-3 py-2 rounded-md text-base font-medium">Services</a>
          <a href="#" className="text-gray-300 hover:bg-gray-700 hover:text-white block px-3 py-2 rounded-md text-base font-medium">Contact</a>
        </div>
      </div>
    </nav>

  );
};

export default Navbar;
