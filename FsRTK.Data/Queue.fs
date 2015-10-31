namespace FsRTK.Data

type Queue<'T> = private {
    Buffer      : 'T list
    Current     : 'T list
} with
    member self.Push x  = { self with Buffer = x :: self.Buffer }
    member self.Pop()   =
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
