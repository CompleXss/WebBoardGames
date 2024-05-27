--
-- PostgreSQL database dump
--

-- Dumped from database version 16.2
-- Dumped by pg_dump version 16.2

-- Started on 2024-05-27 03:13:19

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- TOC entry 236 (class 1255 OID 17235)
-- Name: decrement_user_game_statistics(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.decrement_user_game_statistics() RETURNS trigger
    LANGUAGE plpgsql
    AS $$BEGIN

if OLD is not NULL then

	update user_game_statistics ugs
	set play_count = play_count - 1, win_count = win_count - (case when gp.is_winner is true then 1 else 0 end)
	from game_players gp
	where ugs.game_id = OLD.game_id and ugs.user_id = gp.user_id and gp.game_history_id = OLD.id;

	raise notice 'Updated user_game_statistics (update: decrement)';
end if;

return OLD; -- continue operation

END$$;


--
-- TOC entry 4836 (class 0 OID 0)
-- Dependencies: 236
-- Name: FUNCTION decrement_user_game_statistics(); Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON FUNCTION public.decrement_user_game_statistics() IS 'row-level game_history BEFORE trigger';


--
-- TOC entry 235 (class 1255 OID 17237)
-- Name: increment_user_game_statistics(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.increment_user_game_statistics() RETURNS trigger
    LANGUAGE plpgsql
    AS $$DECLARE
	_user_id			bigint;
	_game_id			integer;
	_win_count_to_add	integer;

BEGIN

if NEW is not NULL then
	_user_id = NEW.user_id;
	_game_id = (select game_id from game_history gh where gh.id = NEW.game_history_id);
	_win_count_to_add = case when NEW.is_winner then 1 else 0 end;
	
	if exists(select 1 from user_game_statistics ugs where ugs.user_id = _user_id and ugs.game_id = _game_id) then
		update user_game_statistics
		set play_count = play_count + 1, win_count = win_count + _win_count_to_add
		where user_id = _user_id and game_id = _game_id;
		
		raise notice 'Updated user_game_statistics (update: increment)';
	else
		insert
		into user_game_statistics (user_id, game_id, play_count, win_count)
		values (_user_id, _game_id, 1, _win_count_to_add);
		
		raise notice 'Updated user_game_statistics (insert)';
	end if;
	
	--raise notice 'user_id: %', _user_id;
	--raise notice 'game_id: %', _game_id;
	--raise notice 'play_count_to_add: %', 1;
	--raise notice 'win_count_to_add: %', _win_count_to_add;
	
end if;

return NEW; -- result is ignored since this is an AFTER trigger

END$$;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 222 (class 1259 OID 16585)
-- Name: game_history; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.game_history (
    id bigint NOT NULL,
    game_id integer NOT NULL,
    date_time_start timestamp(0) with time zone NOT NULL,
    date_time_end timestamp(0) with time zone NOT NULL
);


--
-- TOC entry 221 (class 1259 OID 16584)
-- Name: game_history_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.game_history ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.game_history_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 223 (class 1259 OID 16595)
-- Name: game_players; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.game_players (
    game_history_id bigint NOT NULL,
    user_id bigint NOT NULL,
    is_winner boolean NOT NULL
);


--
-- TOC entry 219 (class 1259 OID 16556)
-- Name: games; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.games (
    id integer NOT NULL,
    name character varying NOT NULL
);


--
-- TOC entry 218 (class 1259 OID 16555)
-- Name: games_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.games ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.games_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 220 (class 1259 OID 16565)
-- Name: user_game_statistics; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.user_game_statistics (
    user_id bigint NOT NULL,
    game_id integer NOT NULL,
    play_count integer DEFAULT 0 NOT NULL,
    win_count integer DEFAULT 0 NOT NULL,
    CONSTRAINT user_game_statistics_play_count_check CHECK ((play_count >= 0)),
    CONSTRAINT user_game_statistics_win_count_check CHECK ((win_count >= 0))
);


--
-- TOC entry 217 (class 1259 OID 16408)
-- Name: user_refresh_token; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.user_refresh_token (
    user_id bigint NOT NULL,
    device_id character(32) NOT NULL,
    refresh_token_hash bytea NOT NULL,
    token_created timestamp(0) with time zone NOT NULL,
    token_expires timestamp(0) with time zone NOT NULL
);


--
-- TOC entry 216 (class 1259 OID 16399)
-- Name: users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.users (
    id bigint NOT NULL,
    public_id character(32) NOT NULL,
    login character varying NOT NULL,
    name character varying NOT NULL,
    password_hash bytea NOT NULL,
    password_salt bytea NOT NULL
);


--
-- TOC entry 215 (class 1259 OID 16398)
-- Name: users_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.users ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.users_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 4677 (class 2606 OID 16589)
-- Name: game_history game_history_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.game_history
    ADD CONSTRAINT game_history_pkey PRIMARY KEY (id);


--
-- TOC entry 4679 (class 2606 OID 16599)
-- Name: game_players game_players_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.game_players
    ADD CONSTRAINT game_players_pkey PRIMARY KEY (game_history_id, user_id);


--
-- TOC entry 4671 (class 2606 OID 16564)
-- Name: games games_name_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.games
    ADD CONSTRAINT games_name_key UNIQUE (name);


--
-- TOC entry 4673 (class 2606 OID 16562)
-- Name: games games_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.games
    ADD CONSTRAINT games_pkey PRIMARY KEY (id);


--
-- TOC entry 4675 (class 2606 OID 16573)
-- Name: user_game_statistics user_game_statistics_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_game_statistics
    ADD CONSTRAINT user_game_statistics_pkey PRIMARY KEY (user_id, game_id);


--
-- TOC entry 4669 (class 2606 OID 16414)
-- Name: user_refresh_token user_refresh_token_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_refresh_token
    ADD CONSTRAINT user_refresh_token_pkey PRIMARY KEY (user_id, device_id);


--
-- TOC entry 4663 (class 2606 OID 16615)
-- Name: users users_login_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_login_key UNIQUE (login);


--
-- TOC entry 4665 (class 2606 OID 16405)
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (id);


--
-- TOC entry 4667 (class 2606 OID 16613)
-- Name: users users_public_id_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_public_id_key UNIQUE (public_id);


--
-- TOC entry 4687 (class 2620 OID 17238)
-- Name: game_players update_user_game_statistics_after_game_players_change; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER update_user_game_statistics_after_game_players_change AFTER INSERT OR DELETE OR UPDATE ON public.game_players FOR EACH ROW EXECUTE FUNCTION public.increment_user_game_statistics();


--
-- TOC entry 4686 (class 2620 OID 17240)
-- Name: game_history update_user_game_statistics_before_game_players_change; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER update_user_game_statistics_before_game_players_change BEFORE DELETE ON public.game_history FOR EACH ROW EXECUTE FUNCTION public.decrement_user_game_statistics();


--
-- TOC entry 4683 (class 2606 OID 16590)
-- Name: game_history game_history_game_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.game_history
    ADD CONSTRAINT game_history_game_id_fkey FOREIGN KEY (game_id) REFERENCES public.games(id) ON DELETE CASCADE;


--
-- TOC entry 4684 (class 2606 OID 16600)
-- Name: game_players game_players_game_history_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.game_players
    ADD CONSTRAINT game_players_game_history_id_fkey FOREIGN KEY (game_history_id) REFERENCES public.game_history(id) ON DELETE CASCADE;


--
-- TOC entry 4685 (class 2606 OID 16605)
-- Name: game_players game_players_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.game_players
    ADD CONSTRAINT game_players_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE SET NULL;


--
-- TOC entry 4681 (class 2606 OID 16579)
-- Name: user_game_statistics user_game_statistics_game_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_game_statistics
    ADD CONSTRAINT user_game_statistics_game_id_fkey FOREIGN KEY (game_id) REFERENCES public.games(id) ON DELETE CASCADE;


--
-- TOC entry 4682 (class 2606 OID 16574)
-- Name: user_game_statistics user_game_statistics_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_game_statistics
    ADD CONSTRAINT user_game_statistics_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- TOC entry 4680 (class 2606 OID 16415)
-- Name: user_refresh_token user_refresh_token_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_refresh_token
    ADD CONSTRAINT user_refresh_token_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


-- Completed on 2024-05-27 03:13:19

--
-- PostgreSQL database dump complete
--

