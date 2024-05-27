import React from 'react';
import { Routes, Route, Navigate, useNavigate } from 'react-router-dom';
import RequireAuth from './components/RequireComponents/RequireAuth';
import NavPanel from './components/NavPanel/navPanel';
import Home from './components/Home/home';
import Login from './components/Login/login';
import Profile from './components/Profile/profile';
import History from './components/History/history';
import Leaderboard from './components/LeaderBoard/leaderboard';
import About from './components/About/about';
import RequireActiveGame from './components/RequireComponents/RequireActiveGame';
import { setNavigateFunc } from './utilities/auth';
import { GameNames } from './utilities/GameNames';
import CheckersLobby from './components/Games/Lobby/checkersLobby';
import MonopolyLobby from './components/Games/Lobby/monopolyLobby';
import CheckersGame from './components/Games/Checkers/checkersGame';
import MonopolyGame from './components/Games/Monopoly/monopolyGame';
import './App.css'

export default function App() {
  setNavigateFunc(useNavigate())

  return <div className='wrapper'>
    <NavPanel />

    <main>
      <Routes>
        <Route path='/' element={<Home />} />
        <Route path='/login' element={<RequireAuth inverse onOk={<Login />} redirect='/profile' />} />
        <Route path='/profile' element={<RequireAuth onOk={<Profile />} />} />
        <Route path='/history' element={<RequireAuth onOk={<History />} />} />
        <Route path='/leaderboard' element={<Leaderboard />} />
        <Route path='/about' element={<About />} />

        <Route path='/lobby/checkers' element={<RequireAuth onOk={<RequireActiveGame gameName={GameNames.checkers} inverse onOk={<CheckersLobby />} redirect='/play/checkers' />} />} />
        <Route path='/lobby/monopoly' element={<RequireAuth onOk={<RequireActiveGame gameName={GameNames.monopoly} inverse onOk={<MonopolyLobby />} redirect='/play/monopoly' />} />} />
        <Route path='/play/checkers' element={<RequireAuth onOk={<RequireActiveGame gameName={GameNames.checkers} onOk={<CheckersGame />} redirect='/lobby/checkers' />} />} />
        <Route path='/play/monopoly' element={<RequireAuth onOk={<RequireActiveGame gameName={GameNames.monopoly} onOk={<MonopolyGame />} redirect='/lobby/monopoly' />} />} />

        <Route path='*' element={<Navigate to={'/'} replace={true} />} />
      </Routes>
    </main>
  </div>
}
