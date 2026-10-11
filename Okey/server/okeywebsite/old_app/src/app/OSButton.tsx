import React from 'react';

const OSInfo = () => {
  const detectOS = () => {
    const platform = navigator.platform;
    if (platform.includes('Win')) {
      return 'Windows';
    } else if (platform.includes('Mac')) {
      return 'MacOS';
    } else if (platform.includes('Linux')) {
      return 'Linux';
    } else {
      return 'Desktop';
    }
  };

  const os = detectOS();

  return (
    <button className="bg-primaryColor hover:bg-primaryColorHover text-black py-2 px-4 rounded">
        Essayer sur {os}
    </button>
  );
};

export default OSInfo;
