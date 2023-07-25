using System.Security.Cryptography;
using ChessChallenge.API;
using System;
public class MyBot : IChessBot
{
    int[] pieceValues = { 0, 100, 280, 320, 500, 900, 10000 };
    ulong whitePawnsBitboard = 0b_0000_0000_1110_0111_1011_1101_0001_1000_0001_1000_0000_0000_0000_0000_0000_0000;
    ulong blackPawnsBitboard = 0b_0000_0000_0000_0000_0000_0000_0001_1000_0001_1000_1011_1101_1111_1111_0000_0000;
    ulong KnightsAndEndKingsBitboard = 0b_0000_0000_0000_0000_0011_1100_0011_1100_0011_1100_0011_1100_0000_0000_0000_0000;
    ulong whiteBishopsBitboard = 0b_1100_0011_1110_0111_0111_1110_0011_1100_0001_1000_0000_0000_0000_0000_0000_0000;
    ulong blackBishopsBitboard = 0b_0000_0000_0000_0000_0000_0000_0001_1000_0011_1100_0111_1110_1110_0111_1100_0011;
    ulong rooksBitboard = 0b_1111_1111_1111_1111_0000_0000_0000_0000_0000_0000_0000_0000_1111_1111_1111_1111;
    ulong whiteKingStartBitboard = 0b_0110_0011_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000;
    ulong blackKingStartBitboard = 0b_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0110_0011;
        public Move Think(Board board, Timer timer)
        {
            Move[] moves = board.GetLegalMoves(false);
            int bestEval = int.MinValue + 1;
            Random rng = new();
            Move moveToPlay = moves[rng.Next(moves.Length)];
            foreach (Move move in moves)
            {
                
                board.MakeMove(move);
                int score = -Megamax(board, ((move.IsCapture || board.IsInCheck()) ? 5 : 3), -bestEval, ((move.IsCapture || board.IsInCheck()) ? -bestEval : int.MaxValue)) - ((!isEndGame(board) && ((int)move.MovePieceType) == 1 && board.PlyCount > 2) ? 50 : 0);
                board.UndoMove(move);
                if (board.GameMoveHistory.Length > 0) if (board.GameMoveHistory[board.GameMoveHistory.Length - 1].MovePieceType == move.MovePieceType) score -= 10;

                if (score > bestEval)
                {
                    bestEval = score;
                    moveToPlay = move;
                }
            }
            return moveToPlay;
        }

    int Megamax(Board board, int depth, int alpha, int beta)
    {
        if (depth == 0 || board.IsDraw() || board.IsInCheckmate())
        {
            return Eval(board);
        }
        int maxEval = int.MinValue + 1;
        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            int score = -Megamax(board, depth - 1, -maxEval, ((move.IsCapture || board.IsInCheck()) ? -beta : int.MaxValue));
            board.UndoMove(move);

            maxEval = Math.Max(maxEval, score);
            if (maxEval > alpha || maxEval > beta) break;
        }
        return maxEval;
    }

        int Eval(Board board)
        {
            if (board.IsInCheckmate()) return (board.IsWhiteToMove) ? int.MinValue : int.MaxValue;
            if (board.IsDraw()) return 0;
            int addOrSubtract = (board.IsWhiteToMove) ? 1 :  -1;
            PieceList[] allpieces = board.GetAllPieceLists();
            int sum = numOfSquaresAttacked(board, true, isEndGame(board)) - numOfSquaresAttacked(board, false, isEndGame(board));
            foreach(PieceList list in allpieces) 
            {
                addOrSubtract = (list.IsWhitePieceList) ? 1 :  -1;
                sum += addOrSubtract * pieceValues[((int)list.TypeOfPieceInList)] * list.Count;
                sum += BitboardHelper.GetNumberOfSetBits(getMatchingBitboard(list.TypeOfPieceInList, list.IsWhitePieceList, board)) * addOrSubtract * 10;
            }
            addOrSubtract = (board.IsWhiteToMove) ? 1 :  -1;
            PieceList whitePawns = board.GetPieceList(((PieceType)1), true);
            PieceList blackPawns = board.GetPieceList(((PieceType)1), false);
            if (board.IsInCheck()) sum -= addOrSubtract * 20;
            if (isEndGame(board))
            {
                if (Math.Abs(sum) >= 300) sum += (7 - Math.Max(Math.Abs(board.GetKingSquare(true).File - board.GetKingSquare(false).File), Math.Abs(board.GetKingSquare(true).Rank - board.GetKingSquare(false).Rank))) * Math.Sign(sum) * 4;
                
                for (int i = 0; i < whitePawns.Count; i++) sum += whitePawns.GetPiece(i).Square.Rank * 3;
                for (int i = 0; i < blackPawns.Count; i++) sum -= (7 - blackPawns.GetPiece(i).Square.Rank) * 3;
                if (board.IsInCheck()) sum -= 40 * addOrSubtract;
            }
            return sum * addOrSubtract;
        }
        ulong getMatchingBitboard(PieceType type, bool white, Board board)
        {
            switch (((int)type)) {
                case 1:
                    return ((white) ? whitePawnsBitboard : blackPawnsBitboard)  & board.GetPieceBitboard(type, white);
                case 2:
                    return KnightsAndEndKingsBitboard & board.GetPieceBitboard(type, white);
                case 3:
                    return ((white) ? whiteBishopsBitboard : blackBishopsBitboard) & board.GetPieceBitboard(type, white);
                case 4:
                    return rooksBitboard & board.GetPieceBitboard(type, white);
                case 5:
                    return 0;
                case 6:
                    return ((isEndGame(board)) ? KnightsAndEndKingsBitboard : ((white) ? whiteKingStartBitboard : blackKingStartBitboard)) & board.GetPieceBitboard(type, white);
                default:
                    return 0;
            }
        }
        bool isEndGame(Board board)
        {
            return BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard) <= 20 || board.PlyCount >= 100;
        }

        int distanceFromCenter(Square square)
        {
            return Math.Max(Math.Min(Math.Abs(4 - square.Rank), Math.Abs(3 - square.Rank)), Math.Min(Math.Abs(4 - square.File), Math.Abs(3 - square.File)));
        }

        int numOfSquaresAttacked(Board board, bool white, bool includeKing)
        {
            ulong result = 0;
            PieceList pieces = board.GetPieceList(((PieceType)1), white);
            for (int i = 0; i < pieces.Count; i++) result |= BitboardHelper.GetPawnAttacks(pieces.GetPiece(i).Square, white);
            pieces = board.GetPieceList(((PieceType)2), white);
            for (int i = 0; i < pieces.Count; i++) result |= BitboardHelper.GetKnightAttacks(pieces.GetPiece(i).Square);
            for (int i = 3; i < 6; i++)
            {
            pieces = board.GetPieceList(((PieceType)i), white);
            for (int j = 0; j < pieces.Count; j++) result |= BitboardHelper.GetSliderAttacks(((PieceType)i), pieces.GetPiece(j).Square, board);
            }
            if (includeKing) result |= BitboardHelper.GetKingAttacks(board.GetKingSquare(white));
            return BitboardHelper.GetNumberOfSetBits(result);
        }
}