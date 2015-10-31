(*
** F# Rendering ToolKit
** Copyright (C) 2015  Wael El Oraiby
** 
** This program is free software: you can redistribute it and/or modify
** it under the terms of the GNU Affero General Public License as
** published by the Free Software Foundation, either version 3 of the
** License, or (at your option) any later version.
** 
** This program is distributed in the hope that it will be useful,
** but WITHOUT ANY WARRANTY; without even the implied warranty of
** MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
** GNU Affero General Public License for more details.
** 
** You should have received a copy of the GNU Affero General Public License
** along with this program.  If not, see <http://www.gnu.org/licenses/>.
*)

namespace FsRTK.Data

type Queue<'T> = private {
    Buffer      : 'T list
    Current     : 'T list
} with
    member self.Enqueue x  = { self with Buffer = x :: self.Buffer }
    member self.Dequeue()   =
        let h, current, buffer =
            match self.Current with
            | h :: t -> h, t, self.Buffer
            | []     ->
                match self.Buffer with
                | [] -> failwith "cannot pop empty queue"
                | _  ->
                    let l = self.Buffer |> List.rev
                    match l with
                    | h :: t -> h, t, []
                    | _ -> failwith "impossible case in queue"
        h, { Current = current; Buffer = buffer }


    member self.Length  = self.Buffer.Length + self.Current.Length

// shadow the type
module Queue =
    let empty = { Buffer = []; Current = [] }
