import OSInfo from './OSButton';
import Hero from './Hero';
import NavBar from './Navbar';
import OkeyHistory from "./History";
import Tutoriel from './Tutoriel';
import Telecharger from './Download';
import Pied from './Footer';

export default function Home() {
  return (
    <div>
        <NavBar />
        <Hero />
        <OkeyHistory />
        <Tutoriel />
        <Telecharger />
        <Pied />
    </div>
  );
}
