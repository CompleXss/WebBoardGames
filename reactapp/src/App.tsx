import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Home from './components/home';
import NavPanel from './components/navPanel';
import History from './components/history';
import Leaderboard from './components/leaderboard';
import About from './components/about';
import Footer from './components/footer';
import './App.css'

export default function App() {
  return (
    <BrowserRouter>
      <div className='wrapper'>
        <NavPanel />

        <main>
          <Routes>
            <Route path='/' element={<Home />} />
            <Route path='/history' element={<History />} />
            <Route path='/leaderboard' element={<Leaderboard />} />
            <Route path='/about' element={<About />} />
            <Route path='*' element={<Navigate to={'/'} replace={true} />} />
          </Routes>
        </main>

        <Footer />
      </div>
    </BrowserRouter>
  );
}
